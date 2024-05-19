using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qz.Utility.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// 转时间戳
        /// </summary>
        /// <param name="date">DateTime 对象</param>
        /// <param name="is10bitSec">
        /// 默认true
        /// true 秒级
        /// false 毫秒级 </param>
        /// <returns></returns>
        public static long ToTimestamp(this DateTime date, bool is10bitSec = true)
        {
            TimeSpan timeSpan = date.ToUniversalTime() - new DateTime(1970, 1, 1);
            if (is10bitSec)
            {
                return (long)timeSpan.TotalSeconds;
            }

            return (long)timeSpan.TotalMilliseconds;
        }
    }
}
