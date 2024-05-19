using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Entity.Models
{
    [Flags]
    public enum ScTaskStatus : int
    {
        [Description("初始化")]
        Inited = 0,

        [Description("执行中")]
        Process = 1,

        [Description("成功")]
        SUCCESS = 2,

        [Description("失败")]
        FAIL = 3,
    }
}
