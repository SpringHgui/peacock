using MQTTnet.Server;
using MQTTnet;
using MQTTnet.Internal;
using System.Text;
using MQTTnet.Protocol;
using System.Timers;
using MQTTnet.Diagnostics;
using Newtonsoft.Json;
using Scheduler.Master.Models;
using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using System.Xml.Linq;
using Microsoft.Win32;

namespace Scheduler.Master.Server
{
    public class MyMqttServer : Disposable
    {
        MqttServer mqttServer;
        IDiscovery discovery;
        MyMqttServerOptions options;
        public string guid;
        System.Timers.Timer discoveryTimer;
        IMqttNetLogger logger;

        IEnumerable<MqttNode> mqttNodes;
        int times = 0;
        public SelfSubscriber selfSubscriber;
        public ConcurrentBag<ExecutorClient> OnlineUsers = new ConcurrentBag<ExecutorClient>();
        IServiceProvider serviceProvider;

        public MyMqttServer(MyMqttServerOptions options, IDiscovery discovery, IMqttNetLogger logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));

            this.serviceProvider = serviceProvider;

            discoveryTimer = new System.Timers.Timer();
            discoveryTimer.Interval = 5000;
            discoveryTimer.Elapsed += discoveryTimerElapsed;

            try
            {
                guid = Guid.NewGuid().ToString();
                var mqttServerOptions = new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithKeepAlive()
                    .WithDefaultEndpointPort(options.Port)
                    .Build();

                // Extend the timestamp for all messages from clients.
                // Protect several topics from being subscribed from every client.

                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.TlsEndpointOptions.IsEnabled = false;

                mqttServerOptions.EnablePersistentSessions = true;

                mqttServer = new MqttFactory().CreateMqttServer(mqttServerOptions, logger);

                const string Filename = "C:\\MQTT\\RetainedMessages.json";

                mqttServer.RetainedMessageChangedAsync += e =>
                {
                    Console.Write($"[RetainedMessage] {e.StoredRetainedMessages}");

                    var directory = Path.GetDirectoryName(Filename);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(Filename, JsonConvert.SerializeObject(e.StoredRetainedMessages));
                    return CompletedTask.Instance;
                };

                mqttServer.RetainedMessagesClearedAsync += e =>
                {
                    Console.Write($"[RetainedMessageCleared]");
                    File.Delete(Filename);
                    return CompletedTask.Instance;
                };

                mqttServer.LoadingRetainedMessageAsync += e =>
                {
                    Console.Write($"[LoadingRetainedMessage]");
                    List<MqttApplicationMessage> retainedMessages = new List<MqttApplicationMessage>();
                    if (File.Exists(Filename))
                    {
                        var json = File.ReadAllText(Filename);
                        retainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
                    }
                    else
                    {
                        retainedMessages = new List<MqttApplicationMessage>();
                    }

                    e.LoadedRetainedMessages = retainedMessages;

                    return CompletedTask.Instance;
                };

                // 发布拦截
                mqttServer.InterceptingPublishAsync += e =>
                {
                    Console.Write($"[InterceptingPublish] 1");
                    if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "/myTopic/WithTimestamp/#") == MqttTopicFilterCompareResult.IsMatch)
                    {
                        // Replace the payload with the timestamp. But also extending a JSON 
                        // based payload with the timestamp is a suitable use case.
                        e.ApplicationMessage.PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("O")));
                    }

                    if (e.ApplicationMessage.Topic == "not_allowed_topic")
                    {
                        e.ProcessPublish = false;
                        e.CloseConnection = true;
                    }

                    return CompletedTask.Instance;
                };

                mqttServer.ClientConnectedAsync += MqttServer_ClientConnectedAsync;
                mqttServer.ClientDisconnectedAsync += MqttServer_ClientDisconnectedAsync;

                // 连接检查
                mqttServer.ValidatingConnectionAsync += e =>
                {
                    Console.Write($"[ValidatingConnection]");
                    if (e.ClientId == "SpecialClient")
                    {
                        if (e.UserName != "USER" || e.Password != "PASS")
                        {
                            e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        }
                    }

                    e.ResponseUserProperties = new List<MQTTnet.Packets.MqttUserProperty>() { new MQTTnet.Packets.MqttUserProperty("server", guid) };

                    return CompletedTask.Instance;
                };

                // 订阅拦截
                mqttServer.InterceptingSubscriptionAsync += e =>
                {
                    Console.Write($"[ValidatingConnection]");
                    //if (e.TopicFilter.Topic.StartsWith("admin/foo/bar") && e.ClientId != "theAdmin")
                    //{
                    //    e.Response.ReasonCode = MqttSubscribeReasonCode.ImplementationSpecificError;
                    //}

                    //if (e.TopicFilter.Topic.StartsWith("the/secret/stuff") && e.ClientId != "Imperator")
                    //{
                    //    e.Response.ReasonCode = MqttSubscribeReasonCode.ImplementationSpecificError;
                    //    e.CloseConnection = true;
                    //}

                    return CompletedTask.Instance;
                };

                mqttServer.InterceptingPublishAsync += e =>
                {
                    Console.Write($"[InterceptingPublish] 2");
                    var payloadText = string.Empty;
                    if (e.ApplicationMessage.PayloadSegment.Count > 0)
                    {
                        payloadText = Encoding.UTF8.GetString(
                            e.ApplicationMessage.PayloadSegment.Array,
                            e.ApplicationMessage.PayloadSegment.Offset,
                            e.ApplicationMessage.PayloadSegment.Count);
                    }

                    MqttNetConsoleLogger.PrintToConsole($"'{e.ClientId}' reported '{e.ApplicationMessage.Topic}' > '{payloadText}'", ConsoleColor.Magenta);
                    return CompletedTask.Instance;
                };

                //options.ApplicationMessageInterceptor = c =>
                //{
                //    if (c.ApplicationMessage.Payload == null || c.ApplicationMessage.Payload.Length == 0)
                //    {
                //        return;
                //    }

                //    try
                //    {
                //        var content = JObject.Parse(Encoding.UTF8.GetString(c.ApplicationMessage.Payload));
                //        var timestampProperty = content.Property("timestamp");
                //        if (timestampProperty != null && timestampProperty.Value.Type == JTokenType.Null)
                //        {
                //            timestampProperty.Value = DateTime.Now.ToString("O");
                //            c.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(content.ToString());
                //        }
                //    }
                //    catch (Exception)
                //    {
                //    }
                //};

                mqttServer.ClientConnectedAsync += e =>
                {
                    Console.Write("Client disconnected event fired.");
                    return CompletedTask.Instance;
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Task MqttServer_ClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
        {
            var clinet = OnlineUsers.FirstOrDefault(x => x.ClientId == arg.ClientId);
            if (clinet != null)
            {
                OnlineUsers.TryTake(out clinet);
            }

            return Task.CompletedTask;
        }

        private Task MqttServer_ClientConnectedAsync(ClientConnectedEventArgs arg)
        {
            OnlineUsers.Add(new ExecutorClient()
            {
                ClientId = arg.ClientId,
                GroupName = arg.UserProperties?.Where(x => x.Name == "GroupName").FirstOrDefault()?.Value,
                StartTime = DateTime.Now,
            });

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await mqttServer.StopAsync();
        }

        public async Task StartAsync()
        {
            discoveryTimer.Start();
            await mqttServer.StartAsync();

            // 自己订阅自己
            selfSubscriber = new SelfSubscriber(this, serviceProvider);
            await selfSubscriber.StartAsync();
        }

        public string ExternalUrl => options.ExternalUrl ?? $"{options.Ip}:{options.Port}";

        IList<ClusterSubscriber> clusterSubscribers = new List<ClusterSubscriber>();

        async Task RegisterAsync()
        {
            // 心跳写入数据库
            discovery.Register(new MqttNode
            {
                Guid = this.guid,
                Endpoint = this.ExternalUrl,
            });

            if (times % 2 == 0)
            {
                // 服务发现
                var res = discovery.Discover();

                Console.WriteLine(JsonConvert.SerializeObject(res));

                res = res.Where(x => x.Guid != guid);

                var newNodes = res.Where(x => !clusterSubscribers.Select(x => x.Guid).Contains(x.Guid));
                var missNodes = clusterSubscribers.Where(x => !res.Select(x => x.Guid).Contains(x.Guid));

                foreach (var item in missNodes)
                {
                    await item.StopAsync();
                    clusterSubscribers.Remove(item);
                }

                foreach (var item in newNodes)
                {
                    var sub = new ClusterSubscriber(item, this);
                    clusterSubscribers.Add(sub);
                    sub.StartAsync();
                }
            }
        }

        void discoveryTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            discoveryTimer.Enabled = false;
            times++;

            try
            {
                RegisterAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                logger.Publish(MqttNetLogLevel.Error, nameof(MyMqttServer), ex.Message, null, ex);
            }
            finally
            {
                discoveryTimer.Enabled = true;
            }
        }
    }
}
