using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Models
{
    public class JobExecutorOptions
    {
        [Required]
        public string Addr { get; set; }

        public string? ClientId { get; set; }

        [Required]
        public string GroupName { get; set; }

        public string? Token { get; set; }
    }
}
