using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scheduler.Core.Executor;
using Scheduler.Core.Models;
using Scheduler.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddScheduler(this IServiceCollection services, IConfigurationSection section, Action<SchedulerConfig> SchedulerConfig)
        {
            var config = new SchedulerConfig(services);
            SchedulerConfig.Invoke(config);

            services.AddSingleton(config);
            services.AddSingleton<JobExecutor>();
            services.AddHostedService<Worker>();
            services.AddOptions<JobExecutorOptions>()
                   .Bind(section).ValidateDataAnnotations();

            return services;
        }
    }
}
