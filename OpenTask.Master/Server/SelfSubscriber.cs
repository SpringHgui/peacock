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
    /// <summary>
    /// 1. 像客户端一样订阅自己，以实现客户端与自己进行通信
    /// 2. 其他server节点与当前节点通信
    /// </summary>
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
                        var id = e.ApplicationMessage.UserProperties?.First(x => x.Name == "id").Value ?? throw new ArgumentNullException();
                        var data = JsonSerializer.Deserialize<string[]>(payloadText) ?? throw new ArgumentNullException();
                        if (mqttServer.CurrentNodeOnlineUsers.TryGetValue(id, out var client))
                        {
                            client.Handelrs = data;

                            // 这个需要在任何节点订阅后，立即受到最后一次数据
                            var msg = new MqttApplicationMessageBuilder()
                                .WithTopic($"server/from/{myMqttServer.guid}/clients-change")
                                .WithPayload(JsonSerializer.Serialize(myMqttServer.CurrentNodeOnlineUsers.Select(x => x.Value)))
                                .WithRetainFlag(true)
                                .Build();

                            PublishAsync(msg);
                        }
                        else
                        {
                            logger.Publish(MqttNetLogLevel.Error, "SyncHandlers", $"未找到对应客户端 {id}", null, null);
                        }

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

                        var res = mqttServer.selfSubscriber.PublishAsync(applicationMessage).Result;
                        break;
                    default:
                        throw new Exception("self 未知的主题");
                }

                return Task.CompletedTask;
            };

            client.ConnectedAsync += async e =>
            {
                // server/316bc382-e64d-4ce6-a82e-c3c535974074/proxy
                var topic = $"client/from/+/#";
                await client.SubscribeAsync(topic);
                Console.WriteLine($"[订阅自己成功] {topic}");
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
