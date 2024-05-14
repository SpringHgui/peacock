using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Models
{
    public class OnJob : BaseMassageAgrs
    {
        public JobInfo Job { get; set; }

        public bool Success { get; set; }

        public string ErrMsg { get; set; }
    }
}
