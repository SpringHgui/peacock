using Microsoft.EntityFrameworkCore;
using Scheduler.Entity.Data;
using Scheduler.Master.Services;
using Scheduler.Service;

namespace Scheduler.Master.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddSchedulerService(this IServiceCollection services, IConfigurationRoot configurationRoot)
        {
            services.AddDbContext<BxjobContext>(options =>
            {
                var constr = configurationRoot.GetSection("DBConnString").GetValue<string>("Job");
                if (constr == null)
                {
                    throw new Exception("未找到数据库连接配置");
                }
                options.EnableSensitiveDataLogging();
                options.UseMySQL(constr);
            });

            services.AddHostedService<SchedulerHostedService>();

            services.AddTransient<ServerService>();
            services.AddTransient<TaskService>();
            services.AddTransient<JobService>();
            services.AddTransient<UserService>();
            return services;
        }
    }
}
