using Microsoft.Extensions.DependencyInjection;
using Scheduler.Core.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Models
{
    public class SchedulerConfig
    {
        IServiceCollection services;

        public SchedulerConfig(IServiceCollection services)
        {
            this.services = services;
        }

        internal Dictionary<string, Type> jobs = new Dictionary<string, Type>();

        public void RegistJobHandler<T>(string name = null)
             where T : class, IJobHandler
        {
            if (name != null && name.Trim().Length == 0)
            {
                throw new ArgumentException("任务名不合法");
            }

            services.AddTransient<T>();

            jobs.Add(name ?? typeof(T).Name, typeof(T));
        }
    }
}
