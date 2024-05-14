namespace Scheduler.Master.Server
{
    public class TestDiscovery : IDiscovery
    {
        List<MyMqttServer> Servers = new List<MyMqttServer>();

        public IEnumerable<MqttNode> Discover()
        {
            return Servers.Select(x => new MqttNode
            {
                Endpoint = x.ExternalUrl,
                Guid = x.guid
            });
        }

        public void Add(MyMqttServer myMqttServer)
        {
            Servers.Add(myMqttServer);
        }

        public void Register(MqttNode mqttNode)
        {
            throw new NotImplementedException();
        }
    }
}
