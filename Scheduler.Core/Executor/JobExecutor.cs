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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using Polly;
using Scheduler.Core.Handlers;
using Scheduler.Core.Models;
using Serilog.Context;

namespace Scheduler.Core.Executor
{
    public class JobExecutor : IJobExecutor
    {
        ILogger<JobExecutor> logger;
        IMqttClient client;
        IServiceProvider serviceProvider;
        JobExecutorOptions schedulerOptions;
        SchedulerConfig schedulerConfig;
        ConcurrentQueue<OnJob> jobsQueue = new ConcurrentQueue<OnJob>();
        MqttClientOptions clientOptions;
        string willTopic => $"sys/client/offline/{ClientId}";
        string ClientId;
        System.Timers.Timer timer;
        public JobExecutor(
            ILogger<JobExecutor> logger,
            IOptions<JobExecutorOptions> options,
            SchedulerConfig schedulerConfig, IMqttNetLogger mqttNetLogger,
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

            var factory = new MqttFactory(mqttNetLogger);

            ClientId = Guid.NewGuid().ToString();
            clientOptions = new MqttClientOptions
            {
                KeepAlivePeriod = TimeSpan.FromSeconds(10),
                ProtocolVersion = MQTTnet.Formatter.MqttProtocolVersion.V500,
                ClientId = ClientId,
                ChannelOptions = new MqttClientTcpOptions // new MqttClientWebSocketOptions { Uri = server };
                {
                    Port = int.Parse(schedulerOptions.Addr.First().Split(':')[1]),
                    Server = schedulerOptions.Addr.First().Split(':')[0]
                },
                UserProperties = new List<MQTTnet.Packets.MqttUserProperty>() {
                    new MQTTnet.Packets.MqttUserProperty("GroupName", schedulerOptions.GroupName)
                },
                // TODO: 账号通过算法生产
                //Credentials = new MqttClientCredentials("", Encoding.UTF8.GetBytes(schedulerOptions.Token)),
                WillTopic = willTopic,
                WillDelayInterval = 5,
                WillPayload = Encoding.UTF8.GetBytes($"Offline"),
            };

            client = factory.CreateMqttClient();

            client.ApplicationMessageReceivedAsync += e =>
            {
                var payloadText = string.Empty;
                if (e.ApplicationMessage.PayloadSegment.Count > 0)
                {
                    payloadText = Encoding.UTF8.GetString(
                        e.ApplicationMessage.PayloadSegment.Array,
                        e.ApplicationMessage.PayloadSegment.Offset,
                        e.ApplicationMessage.PayloadSegment.Count);
                }

                Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($"+ Payload = {payloadText}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine();

                return Task.CompletedTask;
            };

            client.ConnectedAsync += async e =>
            {
                await client.SubscribeAsync($"sys/client/{ClientId}");

                Console.WriteLine($"[{ClientId}] 连接成功");
            };

            client.DisconnectedAsync += async e =>
            {
                Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await client.ConnectAsync(clientOptions);
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            };
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

                    ExecuteAsync(job);
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
        public async Task ExecuteAsync(OnJob msg)
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

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("MyTopic")
                .WithPayload(JsonSerializer.Serialize(msg))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag().Build();

            var res = await client.PublishAsync(message, CancellationToken.None);
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
        public async Task ConnectWithRetryAsync(CancellationToken token)
        {
            try
            {
                await client.ConnectAsync(clientOptions, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "连接失败");
            }
        }
    }
}
