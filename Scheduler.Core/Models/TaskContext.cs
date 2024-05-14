using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Core.Models
{
    public class TaskContext
    {
        public TaskContext(JobInfo job)
        {
            JobInfo = job;
        }

        /// <summary>
        /// 取消任务的令牌
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; } = CancellationToken.None;

        /// <summary>
        /// 任务信息
        /// </summary>
        public JobInfo JobInfo { get; }
    }
}
