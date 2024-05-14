using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScTask
{
    /// <summary>
    /// 调度实例编号
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 任务编号
    /// </summary>
    public long JobId { get; set; }

    /// <summary>
    /// 调度开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 调度结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 结果
    /// </summary>
    public string Result { get; set; } = null!;

    /// <summary>
    /// flags
    /// </summary>
    public int Flags { get; set; }

    /// <summary>
    /// 状态 0:未执行 1:执行中 2:成功 3:失败
    /// </summary>
    public sbyte Status { get; set; }

    /// <summary>
    /// 执行客户端id
    /// </summary>
    public string ClientId { get; set; } = null!;
}
