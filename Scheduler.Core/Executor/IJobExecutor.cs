using Scheduler.Core.Handlers;
using Scheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Core.Executor
{
    public interface IJobExecutor
    {
        void ExecuteAsync(OnJob job);

        Task RunAsync(CancellationToken cancellationToken);
    }
}
