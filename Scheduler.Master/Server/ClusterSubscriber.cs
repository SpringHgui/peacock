using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet;
using MQTTnet.Server;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using System.Threading.Channels;

namespace Scheduler.Master.Server
{
    public class ClusterSubscriber
    {
        IMqttClient client;

        private MqttNode nodeInfo;
        MqttClientOptions clientOptions;
        MyMqttServer mqttServer;
        public string Guid => nodeInfo.Guid;

        string willTopic => $"sys/cluster/offline/{Guid}";

        // 转发主题
        const string CLUSTER_TOPIC = "sys/cluster/proxy";

        public ClusterSubscriber(MqttNode nodeInfo, MyMqttServer mqttServer)
        {
            this.nodeInfo = nodeInfo;
            this.mqttServer = mqttServer;
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
                    Port = int.Parse(nodeInfo.Endpoint.Split(':')[1]),
                    Server = nodeInfo.Endpoint.Split(':')[0]
                },
                // TODO: 账号通过算法生产
                Credentials = new MqttClientCredentials("", Encoding.UTF8.GetBytes("")),
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
                await client.SubscribeAsync(CLUSTER_TOPIC + "/" + Guid);
                Console.WriteLine($"[{mqttServer.guid}] 连接成功 {nodeInfo.Guid}");
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

        async Task ConnectAsync()
        {
            try
            {
                try
                {
                    await client.ConnectAsync(clientOptions);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public async Task StartAsync()
        {
            await ConnectAsync();
        }

        public async Task StopAsync()
        {
            await client.TryDisconnectAsync();
            client.Dispose();
        }
    }
}