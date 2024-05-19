using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Core.Models
{
    public class OnInvoke
    {
        /// <summary>
        /// 请求唯一标识
        /// </summary>
        public string InvokeId { get; set; }

        /// <summary>
        /// 请求接口名
        /// </summary>
        public string ApiName { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Data { get; set; }
    }
}
