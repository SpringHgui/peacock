namespace Scheduler.Master.Models
{
    public class ExecutorClient
    {
        public string ServerId { get; set; }

        public string GroupName { get; set; }

        public string ConnectionId { get; set; }

        public IEnumerable<string> Handelrs { get; set; }

        public DateTime StartTime { get; set; }

        public string ClientId { get; set; }
    }
}
