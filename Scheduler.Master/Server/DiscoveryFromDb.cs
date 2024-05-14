using Qz.Utility.Extensions;
using Scheduler.Master.Services;
using Scheduler.Service;

namespace Scheduler.Master.Server
{
    public class DiscoveryFromDb : IDiscovery
    {
        IServiceProvider service;

        public DiscoveryFromDb(IServiceProvider service)
        {
            this.service = service;
        }

        public IEnumerable<MqttNode> Discover()
        {
            using var scope = this.service.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();

            var list = serverService.GetServerOnline(10);

            return list.Select(x => new MqttNode
            {
                Endpoint = x.EndPoint,
                Guid = x.Guid,
            });
        }

        public void Register(MqttNode mqttNode)
        {
            using var scope = this.service.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
 
            serverService.RegisterOrUpdate(new Entity.Models.ScServer
            {
                Guid = mqttNode.Guid,
                EndPoint = mqttNode.Endpoint,
                HeartAt = DateTime.Now.ToTimestamp(),
            });
        }
    }
}
