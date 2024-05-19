namespace Scheduler.Master.Server
{
    public interface IDiscovery
    {
        public Task StartAsync(MyMqttServer myMqttServer);

        public event OnNewNodeChange OnNewNodeConnected;
        public event OnNewNodeChange OnNodeDisconnected;
        public event OnNodeSlotsChange OnSlotsChange;

        public delegate Task OnNodeSlotsChange(int start, int end);

        public delegate Task OnNewNodeChange(MqttNode node);
    }
}
