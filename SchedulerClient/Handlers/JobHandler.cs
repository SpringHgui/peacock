using Scheduler.Core.Handlers;
using Scheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Client.Handlers
{
    internal class JobHandler : IJobHandler
    {
        public void Run(TaskContext context)
        {
            Console.WriteLine("Job");
            Thread.Sleep(20000);
        }
    }
}
