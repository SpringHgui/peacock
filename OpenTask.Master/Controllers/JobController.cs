using Microsoft.AspNetCore.Mvc;
using Scheduler.Entity.Models;
using Scheduler.Master.Models;
using Scheduler.Master.Services;
using Scheduler.Service;
using System.ComponentModel.DataAnnotations;

namespace Scheduler.Master.Controllers
{
    public class JobController : BaseApiController
    {
        JobService jobService;

        public JobController(JobService jobService)
        {
            this.jobService = jobService;
        }

        [HttpPost]
        public ResultData GetJobList(GetJobListRequest request)
        {
            var list = jobService.ListJobs(request.PageNumber, request.PageSize, request.name, out int count);

            ResultData.data = new
            {
                rows = list,
                count
            };

            return ResultData;
        }

        [HttpPost]
        public ResultData AddJob([FromServices] SchedulerSystem schedulerService, ScJob model)
        {
            var entity = jobService.AddJob(model);
            schedulerService.SchedulerJob(entity);
            return ResultData;
        }

        [HttpDelete]
        public ResultData DeleteJob(long jobId)
        {
            jobService.DelJob(jobId);
            return ResultData;
        }

        [HttpPost]
        public async Task<ResultData> SwitchEnabledStatus(long jobId)
        {
            ResultData.success = await jobService.SwitchEnabledStatus(jobId);
            return ResultData;
        }

        [HttpPost]
        public async Task<ResultData> ExcuteOnce([FromServices] ExcuteJobHandler excuteJobHandler, [Required] long jobId)
        {
            var job = jobService.GetJob(jobId);
            if (job == null)
            {
                throw new Exception("任务不存在");
            }

            var result = await excuteJobHandler.ExcuteJobAsync(job);
            ResultData.success = result.Success;
            ResultData.message = result.Message;

            return ResultData;
        }

        /// <summary>
        /// 获取最近
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ResultData GetNext5executiontimes(long jobId)
        {
            var job = jobService.GetJob(jobId);
            if (job == null)
                throw new Exception("任务不存在");

            var crontab = CrontabUtility.Parse(job.TimeExpression);
            DateTime[] dateTimes = new DateTime[5];
            var current = DateTime.Now;
            for (int i = 0; i < 5; i++)
            {
                dateTimes[i] = crontab.GetNextOccurrence(current);
                current = dateTimes[i];
            }

            ResultData.data = dateTimes.Select(x => x.ToString("yyyy-MM-dd HH:mm:ss"));
            return ResultData;
        }
    }
}
