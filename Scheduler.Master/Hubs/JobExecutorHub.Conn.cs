using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Scheduler.Core;
using Scheduler.Master.Authentication;
using Scheduler.Master.Models;
using Scheduler.Master.Services;
using Scheduler.Service;
using System.Security.Claims;
using System.Threading.Channels;

namespace Scheduler.Master.Hubs
{
    [Authorize(AuthenticationSchemes = MYAuthSchemeConstants.AuthenticationScheme)]
    public partial class JobExecutorHub : Hub
    {
        ILogger<JobExecutorHub> logger;
        IServiceProvider serviceProvider;
        JobService jobService;
        ExcuteJobHandler excuteJobHandler;

        public JobExecutorHub(ILogger<JobExecutorHub> logger, IServiceProvider serviceProvider, ExcuteJobHandler excuteJobHandler, JobService jobService)
        {
            this.excuteJobHandler = excuteJobHandler;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.jobService = jobService;
        }

        string GroupName => getUserProp(ConstString.HEADER_GROUP_NAME);

        string ClientId => getUserProp(ConstString.HEADER_CLIENT_ID);

        string getUserProp(string prop)
        {
            var claim = Context.User?.FindFirst(x => x.Type == prop);
            if (claim == null)
            {
                return null;
            }
            else
            {
                return claim.Value;
            }
        }

        static object locker = new object();

        public override Task OnConnectedAsync()
        {
            if (GroupName != null)
            {
                logger.LogInformation($"[Work节点连接]：{GroupName}");
                lock (locker)
                {
                    this.Groups.AddToGroupAsync(Context.ConnectionId, GroupName);

                    Global.OnlineUsers.Add(new ExecutorClient
                    {
                        ClientId = ClientId,
                        GroupName = GroupName,
                        ConnectionId = Context.ConnectionId,
                        StartTime = DateTime.Now,
                        Handelrs = Enumerable.Empty<string>()
                    });
                }
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (GroupName != null)
            {
                logger.LogInformation($"[集群连接]：{GroupName}");
                lock (locker)
                {
                    var cluster = Global.OnlineUsers.FirstOrDefault(x => x.GroupName == GroupName);
                    if (cluster != null)
                    {
                        Global.OnlineUsers.Remove(cluster);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
