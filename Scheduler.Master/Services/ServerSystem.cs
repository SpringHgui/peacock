using MQTTnet.Diagnostics;
using Scheduler.Master.Server;
using System.Net;

namespace Scheduler.Master.Services
{
    public class ServerSystem
    {
        readonly IMqttNetLogger mqttNetLogger;

        public MyMqttServer myMqttServer { get; }

        public ServerSystem(IDiscovery discovery, IMqttNetLogger mqttNetLogger, IServiceProvider serviceProvider)
        {
            this.mqttNetLogger = mqttNetLogger;

            String strHostName = string.Empty;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray();
            strHostName = addr.First().ToString();

            myMqttServer = new MyMqttServer(new MyMqttServerOptions
            {
                Ip = strHostName,
                Port = 1883,
            }, discovery, this.mqttNetLogger, serviceProvider);
        }

        public async Task StartAsync()
        {
            await myMqttServer.StartAsync();
        }
    }
}
