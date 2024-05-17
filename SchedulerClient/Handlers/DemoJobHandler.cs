using Scheduler.Core.Handlers;
using Scheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Client.Handlers
{
    internal class DemoJobHandler : IJobHandler
    {
        public void Run(TaskContext context)
        {
            Console.WriteLine($"执行参数：{context.JobInfo.JobParams}");
            Console.WriteLine("DemoJob");
            Thread.Sleep(2000);
        }
    }
}
