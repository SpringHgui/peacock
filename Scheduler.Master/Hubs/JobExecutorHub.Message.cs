using Api.WeiXinWork.Robot;
using Api.WeiXinWork.Robot.Models;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Core.Models;
using Scheduler.Entity.Models;
using Scheduler.Master.Services;
using Scheduler.Service;
using System.Text.Json;
using static Mysqlx.Notice.Warning.Types;

namespace Scheduler.Master.Hubs
{
    public partial class JobExecutorHub
    {
        public void OnInvoke(OnInvoke onInvoke)
        {
            if (!excuteJobHandler.Tcs.TryRemove(onInvoke.InvokeId, out TaskCompletionSource<string>? task))
            {
                return;
            }

            task.SetResult(onInvoke.Data);
        }

        /// <summary>
        /// 任务回调
        /// </summary>
        /// <param name="onJob"></param>
        /// <exception cref="Exception"></exception>
        public void OnJob(OnJob onJob)
        {
            logger.LogInformation("OnJob:" + this.GetHashCode().ToString());
            using var scope = serviceProvider.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            logger.LogInformation($"回调：{JsonSerializer.Serialize(onJob)}");

            var task = taskService.GetTaskById(onJob.Job.TaskId);
            if (task == null)
            {
                throw new Exception("task实例不存在");
            }

            task.Result = onJob.ErrMsg;
            task.EndTime = DateTime.Now;

            if (onJob.Success)
                task.Status = (int)ScTaskStatus.SUCCESS;
            else
            {
                task.Status = (int)ScTaskStatus.FAIL;
            }

            taskService.Update(task);
            var job = jobService.GetJob(onJob.Job.JobId);
            if (job == null)
            {
                throw new Exception($"任务不存在：{onJob.Job.JobId}");
            }

            jobService.UpdateParallelCount(onJob.Job.JobId, -1);

            // 报警
            if (!onJob.Success)
            {

                if (job != null && job.AlarmType > 0)
                {
                    switch (job.AlarmType)
                    {
                        case 1:
                            if (!string.IsNullOrEmpty(job.AlarmContent))
                            {
                                var text = $"""
                                    任务调度平台/调度失败提醒
                        
                                    任务名称：{job.GroupName}/{job.Name}
                                    调度实例：TaskId：{onJob.Job.TaskId}
                                    危险级别：{"高"}
                                    提醒时间：{DateTime.Now.ToString("MM-dd HH:mm:ss")}
                                    详细内容：{onJob.ErrMsg}
                        
                                    请值班研发人员查看失败原因，及时处理！
                                    """;

                                var robot = new RobotApi(job.AlarmContent);
                                robot.Send(new SendMsgRequest
                                {
                                    text = new SendMsgRequest.Text
                                    {
                                        content = text
                                    },
                                    msgtype = SendMsgRequest.MsgType.text
                                });
                            }

                            break;
                        default:

                            break;
                    }
                }
            }
        }

        public void SyncHandlers(IEnumerable<string> handlerNames)
        {
            logger.LogInformation("SyncHandlers:" + this.GetHashCode().ToString());
            var client = Global.OnlineUsers.FirstOrDefault(x => x.ConnectionId == this.Context.ConnectionId);
            if (client == null)
            {
                // 这里永远都不应该出现
                throw new Exception($"未找到ConnectionId为{Context.ConnectionId}的客户端");
            }

            client.Handelrs = handlerNames;
        }
    }
}
