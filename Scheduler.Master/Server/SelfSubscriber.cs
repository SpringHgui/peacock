using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet;
using MySqlX.XDevAPI;
using System.Text;
using System.Text.Json;
using Scheduler.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Entity.Models;
using Scheduler.Service;
using System;
using Microsoft.AspNetCore.Hosting.Server;
using System.Threading.Tasks;
using Scheduler.Master.Models;

namespace Scheduler.Master.Server
{
    public class SelfSubscriber
    {
        MyMqttServer mqttServer;
        MqttClientOptions clientOptions;
        IMqttClient client;
        IServiceProvider serviceProvider;

        public SelfSubscriber(MyMqttServer myMqttServer, IServiceProvider serviceProvider)
        {
            this.mqttServer = myMqttServer;
            this.serviceProvider = serviceProvider;
            var logger = new MqttNetEventLogger();
            MqttNetConsoleLogger.ForwardToConsole(logger);

            var factory = new MqttFactory(logger);

            clientOptions = new MqttClientOptions
            {
                KeepAlivePeriod = TimeSpan.FromSeconds(10),
                ProtocolVersion = MQTTnet.Formatter.MqttProtocolVersion.V500,
                ClientId = this.mqttServer.guid,
                ChannelOptions = new MqttClientTcpOptions // new MqttClientWebSocketOptions { Uri = server };
                {
                    Port = int.Parse(mqttServer.ExternalUrl.Split(':')[1]),
                    Server = mqttServer.ExternalUrl.Split(':')[0]
                },
                // TODO: 账号通过算法生产
                Credentials = new MqttClientCredentials("", Encoding.UTF8.GetBytes("")),
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

                var topic = e.ApplicationMessage.Topic.Split("/").Last();

                switch (topic)
                {
                    case "SyncHandlers":
                        var id = e.ApplicationMessage.UserProperties?.First(x => x.Name == "id").Value;
                        var data = JsonSerializer.Deserialize<string[]>(payloadText);
                        mqttServer.CurrentNodeOnlineUsers.First(x => x.ClientId == id).Handelrs = data;
                        break;
                    case "job_reslut":
                        var onJob = JsonSerializer.Deserialize<OnJob>(payloadText);

                        using (var scope = serviceProvider.CreateScope())
                        {
                            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
                            var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

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

                            //// 报警
                            //if (!onJob.Success)
                            //{

                            //    if (job != null && job.AlarmType > 0)
                            //    {
                            //        switch (job.AlarmType)
                            //        {
                            //            case 1:
                            //                if (!string.IsNullOrEmpty(job.AlarmContent))
                            //                {
                            //                    var text = $"""
                            //        任务调度平台/调度失败提醒

                            //        任务名称：{job.GroupName}/{job.Name}
                            //        调度实例：TaskId：{onJob.Job.TaskId}
                            //        危险级别：{"高"}
                            //        提醒时间：{DateTime.Now.ToString("MM-dd HH:mm:ss")}
                            //        详细内容：{onJob.ErrMsg}

                            //        请值班研发人员查看失败原因，及时处理！
                            //        """;

                            //                    var robot = new RobotApi(job.AlarmContent);
                            //                    robot.Send(new SendMsgRequest
                            //                    {
                            //                        text = new SendMsgRequest.Text
                            //                        {
                            //                            content = text
                            //                        },
                            //                        msgtype = SendMsgRequest.MsgType.text
                            //                    });
                            //                }

                            //                break;
                            //            default:

                            //                break;
                            //        }
                            //    }
                            //}
                        }
                        break;
                    case "proxy":
                        var proxy = JsonSerializer.Deserialize<ProxyModel>(payloadText);
                        if (proxy == null)
                            throw new Exception("解析失败");

                        var applicationMessage = new MqttApplicationMessageBuilder()
                          .WithTopic(proxy.topic)
                          .WithPayload(proxy.data)
                        .Build();

                        var res = PublishAsync(applicationMessage).Result;
                        break;
                    default:
                        break;
                }

                return Task.CompletedTask;
            };

            client.ConnectedAsync += async e =>
            {
                await client.SubscribeAsync($"server/{mqttServer.guid}/#");
                Console.WriteLine($"[{mqttServer.guid}] 连接成功");
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

        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await client.PublishAsync(applicationMessage, CancellationToken.None);
        }

        public async Task StartAsync()
        {
            try
            {
                await client.ConnectAsync(clientOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
