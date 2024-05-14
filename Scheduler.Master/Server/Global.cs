using Microsoft.AspNetCore.SignalR;
using Scheduler.Master.Models;

namespace Scheduler.Master.Hubs
{
    public class Global
    {
        public static IList<ExecutorClient> OnlineUsers = new List<ExecutorClient>();
    }
}
