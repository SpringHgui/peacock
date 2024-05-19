using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scheduler.Master.Models
{
    public class ResultData
    {
        public ResultData()
        {
            success = true;
        }

        public bool success { get; set; }

        public string message { get; set; }


        public object data { get; set; }

        /// <summary>
        /// 链路追踪标识
        /// </summary>
        public string trace_id { get; set; }
    }
}
