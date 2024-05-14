using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scheduler.Core.Executor;
using Scheduler.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Core.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    JobExecutor jobExecutor;

    public Worker(ILogger<Worker> logger, JobExecutor jobExecutor)
    {
        this._logger = logger;
        this.jobExecutor = jobExecutor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobExecutor.RunAsync(stoppingToken);
    }
}
