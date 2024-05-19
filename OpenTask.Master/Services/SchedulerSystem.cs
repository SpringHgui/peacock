using Microsoft.Extensions.Logging;
using Qz.Utility.Extensions;
using Scheduler.Entity.Models;
using Scheduler.Master.Models;
using Scheduler.Service;
using TimeCrontab;
using WheelTimer;
using WheelTimer.Utilities;

namespace Scheduler.Master.Services
{
    public class SchedulerSystem
    {
        static HashedWheelTimer hashedWheelTimer;
        ILogger<SchedulerSystem> logger;
        IServiceProvider service;

        System.Timers.Timer timer;
        ExcuteJobHandler excuteJobHandler;

        public void Start(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
            {
                timer.Stop();
                if (hashedWheelTimer != null)
                {
                    hashedWheelTimer.StopAsync().Wait();
                }
            });

            ReloadScheduler();
        }

        ServerSystem serverSystem;

        public SchedulerSystem(ExcuteJobHandler excuteJobHandler,
            IServiceProvider service, ILogger<SchedulerSystem> logger, ServerSystem serverSystem)
        {
            hashedWheelTimer = new HashedWheelTimer();
            this.serverSystem = serverSystem;
            this.excuteJobHandler = excuteJobHandler;
            this.service = service;
            this.logger = logger;

            timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = 15000,// 15秒
            };

            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            logger.LogInformation($"重新加载配置 {serverSystem.myMqttServer.Slot.Start},{serverSystem.myMqttServer.Slot.End}");
            ReloadScheduler();
        }

        public void ReloadScheduler()
        {
            try
            {
                using var scope = service.CreateScope();

                var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
                var jobs = jobService.GetNextJob(serverSystem.myMqttServer.Slot.Start, serverSystem.myMqttServer.Slot.End, DateTime.Now.AddSeconds(15).ToTimestamp());

                foreach (var item in jobs)
                {
                    if (!item.Enabled)
                    {
                        continue;
                    }

                    SchedulerJob(item);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ReloadScheduler Error");
            }
        }

        public void SchedulerJob(ScJob jobMolde)
        {
            try
            {
                var crontab = CrontabUtility.Parse(jobMolde.TimeExpression);
                var nextOccurrence = crontab.GetNextOccurrence(DateTime.Now);

                var delay = nextOccurrence - DateTime.Now;
                logger.LogInformation($"[任务计划]: {jobMolde.Name} {delay.TotalSeconds}s后执行");

                hashedWheelTimer.NewTimeout(new ActionTimerTask(async (job) =>
                {
                    logger.LogInformation($"[任务执行]: {jobMolde.Name} {jobMolde.Description}");

                    // 查询当前任务状态，如果未启用，则不再执行，也停止下个周期的调度
                    using var scope = service.CreateScope();
                    var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

                    // 重新获取，避免在等待执行期间数据库中的数据被修改导致不一致
                    if (jobMolde == null)
                    {
                        logger.LogError($"任务不存：{jobMolde.JobId}");
                        return;
                    }

                    // 
                    if (!jobMolde.Enabled)
                    {
                        logger.LogError($"任务已禁用：{jobMolde.JobId}");
                        return;
                    }

                    // 继续插入下个周期
                    //SchedulerJob(currentJob);
                    var nextOccurrence = crontab.GetNextOccurrence(DateTime.Now);

                    jobMolde.NextTriggerTime = nextOccurrence.ToTimestamp();
                    jobService.UpdateNext(jobMolde);

                    try
                    {
                        var result = await excuteJobHandler.ExcuteJobAsync(jobMolde);
                        logger.LogInformation($"[下发调度任务结果]：{result.Message}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "调度失败");
                    }
                }), delay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "任务调度失败");
            }
        }
    }
}
