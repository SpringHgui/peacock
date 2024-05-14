using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Models
{
    public class ExecutingJobsResponse
    {
        public string Content { get; set; }

        public string Name { get; set; }

        public long JobId { get; set; }

    }
}
