namespace Scheduler.Master.Server
{
    public interface IDiscovery
    {
        public Task StartAsync(MyMqttServer myMqttServer);

        public event OnNewNodeChange OnNewNodeConnected;
        public event OnNewNodeChange OnNodeDisconnected;

        public delegate Task OnNewNodeChange(MqttNode node);
    }
}
