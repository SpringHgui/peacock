using MQTTnet.Diagnostics;
using Scheduler.Master.Server;
using System.Net;

namespace Scheduler.Master.Services
{
    public class MqttServerService : BackgroundService
    {
        ServerSystem server;

        public MqttServerService(ServerSystem server)
        {
            this.server = server;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await server.StartAsync();
        }
    }
}
