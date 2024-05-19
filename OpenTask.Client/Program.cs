using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scheduler.Client.Handlers;
using Scheduler.Core.Executor;
using Scheduler.Core.Extensions;
using Scheduler.Core.Models;
using Scheduler.Core.Services;
using Serilog;

namespace Scheduler.Client
{
    internal class Program
    {
        //static void Main(string[] args)
        //{
        //    var cancellationTokenSource = new CancellationTokenSource();
        //    var logger = LoggerFactory.Create(builder =>
        //    {
        //        builder.SetMinimumLevel(LogLevel.Debug);
        //        builder.AddConsole();
        //    }).CreateLogger<JobExecutor>();

        //    IJobExecutor executor = new JobExecutor(logger, new JobExecutorOptions
        //    {
        //        addr = "https://localhost:7148/hubs/executor",
        //        ClientId = $"{Environment.MachineName}@{Guid.NewGuid().ToString()}",
        //        GroupName = "group2",
        //    });

        //    executor.RegistJobHandler("demoJobHandler", new DemoJobHandler());
        //    executor.RunAsync(cancellationTokenSource.Token);

        //    Console.ReadLine();
        //}

        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                services.AddScheduler(ctx.Configuration.GetSection("Scheduler"), options =>
                {
                    options.RegistJobHandler<DemoJobHandler>();
                    options.RegistJobHandler<JobHandler>();
                });
            }).UseSerilog((context, configuration) =>
            {
                configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Debug);
            })
            .Build();

            host.Run();
        }
    }
}