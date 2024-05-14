using Microsoft.AspNetCore.SignalR;
using Scheduler.Core.Models;
using Scheduler.Entity.Models;
using Scheduler.Service;
using TimeCrontab;
using WheelTimer;
using WheelTimer.Utilities;

namespace Scheduler.Master.Services
{
    public class SchedulerHostedService : BackgroundService
    {
        ILogger<SchedulerHostedService> logger;
        SchedulerSystem schedulerService;

        public SchedulerHostedService(
            ILogger<SchedulerHostedService> logger, SchedulerSystem schedulerService)
        {
            this.logger = logger;
            this.schedulerService = schedulerService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"服务启动：{GetHashCode()}");

            return Task.Run(() =>
            {
                schedulerService.Start(stoppingToken);
            });
        }
    }
}
