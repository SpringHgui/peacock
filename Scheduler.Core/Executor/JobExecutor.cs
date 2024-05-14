using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Scheduler.Core.Handlers;
using Scheduler.Core.Models;
using Serilog.Context;

namespace Scheduler.Core.Executor
{
    public class JobExecutor : IJobExecutor
    {
        ILogger<JobExecutor> logger;
        HubConnection connection;
        IServiceProvider serviceProvider;
        JobExecutorOptions schedulerOptions;
        SchedulerConfig schedulerConfig;
        ConcurrentQueue<OnJob> jobsQueue = new ConcurrentQueue<OnJob>();

        System.Timers.Timer timer;
        public JobExecutor(
            ILogger<JobExecutor> logger,
            IOptions<JobExecutorOptions> options,
            SchedulerConfig schedulerConfig,
            IServiceProvider serviceProvider)
        {
            this.schedulerConfig = schedulerConfig;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.schedulerOptions = options.Value;

            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 1000;
            timer.Start();

            connection = new HubConnectionBuilder()
            .WithUrl(schedulerOptions.Addr, options =>
            {
                options.Headers.Add(ConstString.HEADER_CLIENT_ID, $"{Environment.MachineName}@{Guid.NewGuid().ToString()}");
                options.Headers.Add(ConstString.HEADER_GROUP_NAME, schedulerOptions.GroupName);
                options.Headers.Add(ConstString.HEADER_TOKEN, schedulerOptions.Token);
            })
            //.WithAutomaticReconnect() // 自动重连4次，失败后触发 Closed
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
            })
            .Build();

            connection.Closed += async (ex) =>
            {
                logger.LogError(ex, "[JobExecutor][连接关闭] 5s后重试");
                ConnectWithRetryAsync(cancellationToken).Wait();
            };

            connection.Reconnecting += error =>
            {
                // Notify users the connection was lost and the client is reconnecting.
                // Start queuing or dropping messages.
                logger.LogInformation("[JobExecutor][Reconnecting]");
                return Task.CompletedTask;
            };

            connection.On<OnJob>(nameof(OnJob), (msg) =>
            {
                using var _ = LogContext.PushProperty("RequestId", $"ExecuteTask:{msg.Job.TaskId}");

                logger.LogInformation($"[OnJob] Onjob {JsonSerializer.Serialize(msg)}");
                jobsQueue.Enqueue(msg);
                logger.LogInformation($"当前待处理: {jobsQueue.Count}");
            });

            connection.On<OnInvoke>(nameof(OnInvoke), (msg) =>
            {
                switch (msg.ApiName)
                {
                    case "ExecutingJobs":
                        msg.Data = JsonSerializer.Serialize(ExecutingJobs.Select(x => new ExecutingJobsResponse
                        {
                            Content = x.Job.Content,
                            Name = x.Job.Name,
                            JobId = x.Job.JobId
                        }).ToArray());

                        connection.SendAsync(nameof(OnInvoke), msg);
                        break;
                    default:
                        break;
                }

                logger.LogInformation($"[OnInvok]: {JsonSerializer.Serialize(msg)}");
            });
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;

            try
            {
                if (jobsQueue.Count == 0)
                    return;

                logger.LogInformation($"本次需处理: {jobsQueue.Count}");
                while (jobsQueue.TryDequeue(out OnJob job))
                {
                    using var _ = LogContext.PushProperty("RequestId", $"ExecuteTask:{job.Job.TaskId}");
                    logger.LogInformation($"任务出队: {job.Job.TaskId}");
                    Task.Run(() =>
                    {
                        ExecuteAsync(job);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理任务");
            }
            finally
            {
                timer.Enabled = true;
            }
        }

        public void CancelTask()
        {

        }

        /// <summary>
        /// 正在执行的任务列表
        /// </summary>
        List<OnJob> ExecutingJobs = new List<OnJob>();

        /// <summary>
        /// 执行job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public void ExecuteAsync(OnJob msg)
        {
            var job = msg.Job;

            try
            {
                ExecutingJobs.Add(msg);

                if (!schedulerConfig.jobs.TryGetValue(job.Content, out Type? jobType))
                {
                    throw new Exception($"jobName:{job.Name} -> {job.Content} 未注册");
                }

                var jobHandler = serviceProvider.GetService(jobType) as IJobHandler;

                logger.LogInformation($"[执行前] {job.Name}");

                var ctx = new TaskContext(job);

                if (job.MaxAttempt > 0 && job.AttemptInterval > 0)
                {
                    var policyWrap = Policy
                        .Wrap(Policy
                              .Handle<Exception>()
                              .Retry(job.MaxAttempt, async (exception, retryCount, context) =>
                              {
                                  logger.LogError($"[{job.Name}] 执行异常 {exception}");
                                  await Task.Delay(TimeSpan.FromSeconds(job.AttemptInterval));
                                  logger.LogInformation($"[{job.Name}] 开始第{retryCount}次重试");
                              }), Policy.Timeout(TimeSpan.FromHours(6)));

                    policyWrap.Execute(() =>
                    {
                        jobHandler!.Run(ctx);
                    });
                }
                else
                {
                    jobHandler!.Run(ctx);
                }

                logger.LogInformation($"[执行后] {job.Name}");

                msg.ErrMsg = "成功";
                msg.Success = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[任务执行异常]");
                msg.Success = false;
                msg.ErrMsg = ex.Message;
            }
            finally
            {
                ExecutingJobs.Remove(msg);
            }

            connection.SendAsync(nameof(OnJob), msg);
        }

        CancellationToken cancellationToken;

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            logger.LogInformation("[Scheduler 启动成功]");

            // 开始连接
            await ConnectWithRetryAsync(cancellationToken);
        }

        /// <summary>
        /// 断线重连
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> ConnectWithRetryAsync(CancellationToken token)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            // Keep trying to until we can start or the token is canceled.
            while (true)
            {
                try
                {
                    // 开始连接
                    logger.LogInformation("[开始连接]");
                    await connection.StartAsync(token);
                    Debug.Assert(connection.State == HubConnectionState.Connected);

                    logger.LogInformation("[连接成功]");

                    // 发送当前客户端支持的IjobHandler
                    await connection.SendAsync("SyncHandlers", schedulerConfig.jobs.Select(x => x.Key));
                    return true;
                }
                catch when (token.IsCancellationRequested)
                {
                    logger.LogError("[Connect Stop because CancellationRequested]");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"[连接失败] {ex.Message} trying again in 5000 ms");

                    Debug.Assert(connection.State == HubConnectionState.Disconnected);
                    await Task.Delay(5000);
                }
            }
        }
    }
}
