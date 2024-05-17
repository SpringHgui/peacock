using System.Text.Json.Serialization;

namespace Scheduler.Master.Models
{
    public class ExecutorClient
    {
        [JsonIgnore]
        public string ServerId { get; set; }

        public string GroupName { get; set; }

        public string ConnectionId { get; set; }

        public IEnumerable<string> Handelrs { get; set; }

        public DateTime StartTime { get; set; }

        public string ClientId { get; set; }
    }
}
