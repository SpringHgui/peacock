using MQTTnet;
using Scheduler.Core.Models;
using Scheduler.Entity.Models;
using Scheduler.Master.Models;
using Scheduler.Service;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Scheduler.Master.Services
{
    public class ExcuteJobHandler
    {
        ILogger<ExcuteJobHandler> logger;
        ServerSystem server;
        IServiceProvider service;

        public ExcuteJobHandler(
            ILogger<ExcuteJobHandler> logger, ServerSystem hubContext,
            IServiceProvider service)
        {
            this.logger = logger;
            this.service = service;
            this.server = hubContext;
        }

        public ConcurrentDictionary<string, TaskCompletionSource<string>> Tcs = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public async Task<(bool Success, string Message)> ExcuteJobAsync(ScJob job)
        {
            using var scope = service.CreateScope();
            var task = new ScTask
            {
                JobId = job.JobId,
                StartTime = DateTime.Now,
                Status = 1
            };


            var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

            try
            {
                var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();

                var groupClients = server.myMqttServer.GetClientsByAppName(job.GroupName).ToList();
                logger.LogInformation($"找到{groupClients.Count()}个 {job.GroupName} 的执行节点 ");
                if (!groupClients.Any())
                {
                    var result = $"组[{job.GroupName}]没有在线的执行器";
                    task.Result = result;
                    task.Status = 3;
                    task.Flags |= (int)ScTaskStatus.FAIL;
                    taskService.AddTask(task);
                    return (false, result);
                }

                groupClients = groupClients.Where(x => x.Handelrs != null && x.Handelrs.Contains(job.Content)).ToList();
                logger.LogInformation($"{groupClients.Count()}个执行节点支持 {job.Content}");

                if (!groupClients.Any())
                {
                    var result = $"组[{job.GroupName}]没有支持`{job.Content}`的执行器";
                    task.Result = result;
                    task.Flags |= (int)ScTaskStatus.FAIL;
                    taskService.AddTask(task);
                    return (false, result);
                }

                //if (job.MaxThread > 0)
                //{
                //    int count = 0;
                //    foreach (var groupClient in groupClients)
                //    {
                //        var id = Guid.NewGuid().ToString();
                //        TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
                //        Tcs.TryAdd(id, taskCompletionSource);

                //        try
                //        {
                //            var clinet = server.GetClient(groupClient.ConnectionId);
                //            await clinet.SendAsync(nameof(OnInvoke), new OnInvoke
                //            {
                //                InvokeId = id,
                //                ApiName = "ExecutingJobs",
                //                Data = ""
                //            });

                //            var json = await taskCompletionSource.Task;
                //            var list = JsonSerializer.Deserialize<List<ExecutingJobsResponse>>(json);
                //            var runCount = list.Where(x => x.JobId == job.JobId).Count();

                //            count += runCount;
                //            LogHelper.Info($"{groupClient.ClientId}: {runCount}");
                //        }
                //        catch (Exception)
                //        {
                //            throw;
                //        }
                //        finally
                //        {
                //            Tcs.TryRemove(id, out _);
                //        }
                //    }

                //    if (count >= job.MaxThread)
                //    {
                //        return (false, $"并发数达限制 Max：{job.MaxThread} Current: {count}");
                //    }
                //}

                IEnumerable<ExecutorClient> executors = null;
                if (job.ExecuteMode == "alone")
                {
                    // TODO：繁忙检查，选择一个当前相对空闲的客户端分配任务
                    var index = new Random().Next(0, 1000) % groupClients.Count();
                    executors = groupClients.Where(x => x.ConnectionId == groupClients[index].ConnectionId);
                }
                else if (job.ExecuteMode == "sphere")
                {
                    executors = groupClients;
                }
                else
                {
                    throw new Exception($"运行模式参数异常: 未知的模式{job.ExecuteMode}");
                }

                logger.LogInformation($"以下节点需要下发通知 {JsonSerializer.Serialize(executors)}");

                foreach (var executor in executors)
                {
                    task.ClientId = executor.ClientId;

                    //var clinet = server.GetClient(executor.ConnectionId);
                    //if (clinet == null)
                    //{
                    //    var result = $"ConnectionId: [{executor.ConnectionId}] 竟然不在线";
                    //    task.Result = result;
                    //    task.Flags |= (int)ScTaskStatus.FAIL;
                    //    taskService.AddTask(task);

                    //    // 这个分支不应该出现
                    //    return (false, result);
                    //}

                    task = taskService.AddTask(task);

                    logger.LogInformation($"任务创建完成: {task.TaskId}，clinetId：{executor.ClientId}");

                    jobService.UpdateParallelCount(job.JobId, +1);

                    if (executor.ServerId == server.myMqttServer.guid)
                    {
                        logger.LogInformation($"工作节点在当前服务器");
                        var applicationMessage = new MqttApplicationMessageBuilder()
                           .WithTopic($"client/to/{executor.ClientId}/onjob")
                           .WithPayload(JsonSerializer.Serialize(new OnJob
                           {
                               Job = new JobInfo
                               {
                                   TaskId = task.TaskId,
                                   Name = job.Name,
                                   GroupName = job.GroupName,
                                   TimeExpression = job.TimeExpression,
                                   TimeType = job.TimeType,
                                   MaxAttempt = job.MaxAttempt,
                                   AttemptInterval = job.AttemptInterval,
                                   Content = job.Content,
                                   Description = job.Description,
                                   ExecuteMode = job.ExecuteMode,
                                   JobId = job.JobId,
                                   JobParams = job.JobParams
                               }
                           }))
                           .Build();

                        await server.myMqttServer.selfSubscriber.PublishAsync(applicationMessage);
                        taskService.UpdateTaskFlag(task.TaskId, ScTaskStatus.Process);
                    }
                    else
                    {
                        logger.LogInformation($"工作节点在 {executor.ServerId} 开始发送转发主题");

                        // 转发到对应server进行
                        var applicationMessage = new MqttApplicationMessageBuilder()
                           .WithTopic($"server/from/{server.myMqttServer.guid}/proxy")
                           .WithPayload(JsonSerializer.Serialize(new ProxyModel
                           {
                               topic = $"client/to/{executor.ClientId}/onjob",
                               data = JsonSerializer.Serialize(new OnJob
                               {
                                   Job = new JobInfo
                                   {
                                       TaskId = task.TaskId,
                                       Name = job.Name,
                                       GroupName = job.GroupName,
                                       TimeExpression = job.TimeExpression,
                                       TimeType = job.TimeType,
                                       MaxAttempt = job.MaxAttempt,
                                       AttemptInterval = job.AttemptInterval,
                                       Content = job.Content,
                                       Description = job.Description,
                                       ExecuteMode = job.ExecuteMode,
                                       JobId = job.JobId,
                                       JobParams = job.JobParams
                                   }
                               })
                           }
                          ))
                           .Build();

                        await server.myMqttServer.selfSubscriber.PublishAsync(applicationMessage);
                    }

                    taskService.UpdateTaskFlag(task.TaskId, ScTaskStatus.Process);
                }

                return (true, "指令发送成功");
            }
            catch
            {
                jobService.UpdateParallelCount(job.JobId, -1);
                throw;
            }
        }
    }
}
