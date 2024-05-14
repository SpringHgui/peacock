using Microsoft.Extensions.Logging;
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

        static object locker = new object();
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

        public SchedulerSystem(ExcuteJobHandler excuteJobHandler,
            IServiceProvider service, ILogger<SchedulerSystem> logger)
        {
            this.excuteJobHandler = excuteJobHandler;
            this.service = service;
            this.logger = logger;

            timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = 60 * 5 * 1000,// 5分钟执行一次 60 * 5 * 1000
            };

            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            logger.LogInformation("重新加载配置");
            ReloadScheduler();
        }

        public void ReloadScheduler()
        {
            lock (locker)
            {
                try
                {

                    if (hashedWheelTimer != null)
                        hashedWheelTimer.StopAsync().Wait();

                    hashedWheelTimer = new HashedWheelTimer();

                    using var scope = service.CreateScope();

                    var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
                    var jobs = jobService.ListNextJobs();

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
                    var currentJob = jobService.GetJob(jobMolde.JobId);
                    if (currentJob == null)
                    {
                        logger.LogError($"任务不存：{jobMolde.JobId}");
                        return;
                    }

                    // 
                    if (!currentJob.Enabled)
                    {
                        logger.LogError($"任务已禁用：{jobMolde.JobId}");
                        return;
                    }

                    // 继续插入下个周期
                    SchedulerJob(currentJob);

                    try
                    {
                        var result = await excuteJobHandler.ExcuteJobAsync(currentJob);
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
