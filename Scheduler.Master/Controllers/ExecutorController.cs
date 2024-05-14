using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Scheduler.Core;
using Scheduler.Core.Models;
using Scheduler.Master.Models;
using Scheduler.Master.Services;

namespace Scheduler.Master.Controllers
{
    public class ExecutorController : BaseApiController
    {
        private readonly ILogger<ExecutorController> _logger;

        public ExecutorController(ILogger<ExecutorController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ResultData GetOnlineExecutor()
        {
            //ResultData.data = Global.OnlineUsers.Select(x => new
            //{
            //    StartTime = x.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            //    x.ClientId,
            //    x.GroupName,
            //    x.Handelrs
            //});
            return ResultData;
        }
    }
}