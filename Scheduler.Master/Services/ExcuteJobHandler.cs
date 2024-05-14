using BX.Utility.Helper;
using Microsoft.AspNetCore.SignalR;
using Mysqlx.Prepare;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using Scheduler.Core.Models;
using Scheduler.Entity.Models;
using Scheduler.Master.Hubs;
using Scheduler.Master.Models;
using Scheduler.Master.Server;
using Scheduler.Service;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;

namespace Scheduler.Master.Services
{
    public class ExcuteJobHandler
    {
        ILogger<ExcuteJobHandler> logger;
        HubContext hubContext;
        IServiceProvider service;

        public ExcuteJobHandler(
            ILogger<ExcuteJobHandler> logger, HubContext hubContext,
            IServiceProvider service)
        {
            this.logger = logger;
            this.service = service;
            this.hubContext = hubContext;
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

                var groupClients = Global.OnlineUsers.Where(x => x.GroupName == job.GroupName).ToList();
                if (!groupClients.Any())
                {
                    var result = $"组[{job.GroupName}]没有在线的执行器";
                    task.Result = result;
                    task.Status = 3;
                    task.Flags |= (int)ScTaskStatus.FAIL;
                    taskService.AddTask(task);
                    return (false, result);
                }

                groupClients = groupClients.Where(x => x.Handelrs.Contains(job.Content)).ToList();
                if (!groupClients.Any())
                {
                    var result = $"组[{job.GroupName}]没有支持`{job.Content}`的执行器";
                    task.Result = result;
                    task.Flags |= (int)ScTaskStatus.FAIL;
                    taskService.AddTask(task);
                    return (false, result);
                }

                if (job.MaxThread > 0)
                {
                    int count = 0;
                    foreach (var groupClient in groupClients)
                    {
                        var id = Guid.NewGuid().ToString();
                        TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
                        Tcs.TryAdd(id, taskCompletionSource);

                        try
                        {
                            var clinet = hubContext.GetClient(groupClient.ConnectionId);
                            await clinet.SendAsync(nameof(OnInvoke), new OnInvoke
                            {
                                InvokeId = id,
                                ApiName = "ExecutingJobs",
                                Data = ""
                            });

                            var json = await taskCompletionSource.Task;
                            var list = JsonSerializer.Deserialize<List<ExecutingJobsResponse>>(json);
                            var runCount = list.Where(x => x.JobId == job.JobId).Count();

                            count += runCount;
                            LogHelper.Info($"{groupClient.ClientId}: {runCount}");
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        finally
                        {
                            Tcs.TryRemove(id, out _);
                        }
                    }

                    if (count >= job.MaxThread)
                    {
                        return (false, $"并发数达限制 Max：{job.MaxThread} Current: {count}");
                    }
                }

                IEnumerable<ExecutorClient> executors = null;
                if (job.ExecuteMode == "alone")
                {
                    // TODO：繁忙检查，选择一个当前相对空闲的客户端分配任务
                    var index = new Random().Next(groupClients.Count());
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

                foreach (var executor in executors)
                {
                    task.ClientId = executor.ClientId;

                    var clinet = hubContext.GetClient(executor.ConnectionId);
                    if (clinet == null)
                    {
                        var result = $"ConnectionId: [{executor.ConnectionId}] 竟然不在线";
                        task.Result = result;
                        task.Flags |= (int)ScTaskStatus.FAIL;
                        taskService.AddTask(task);

                        // 这个分支不应该出现
                        return (false, result);
                    }

                    task = taskService.AddTask(task);

                    logger.LogInformation($"任务创建完成: {task.TaskId}，clinetId：{executor.ClientId}");

                    jobService.UpdateParallelCount(job.JobId, +1);
                    await clinet.SendAsync(nameof(OnJob), new OnJob
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
                    });

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
