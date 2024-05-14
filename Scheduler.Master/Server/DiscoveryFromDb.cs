using Scheduler.Master.Services;

namespace Scheduler.Master.Server
{
    public class DiscoveryFromDb : IDiscovery
    {
        public IEnumerable<MqttNode> Discover()
        {
            return new MqttNode[] { };
        }
    }
}
