﻿namespace Scheduler.Master.Server
{
    public interface IDiscovery
    {
        public IEnumerable<MqttNode> Discover();
    }
}
