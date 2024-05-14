using Scheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Handlers
{
    public interface IJobHandler
    {
        public void Run(TaskContext context);
    }
}
