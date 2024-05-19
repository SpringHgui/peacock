namespace Scheduler.Master.Server
{
    public class MqttNode
    {
        public string Endpoint { get; set; }

        public string Guid { get; set; }

        /// <summary>
        /// 0~16383
        /// </summary>
        public string? Slot { get; set; }

        public long Id { get; set; }

        public long HeartAt { get; set; }
    }
}
