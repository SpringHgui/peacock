using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScJob
{
    public long JobId { get; set; }

    /// <summary>
    /// 分组名
    /// </summary>
    public string GroupName { get; set; } = null!;

    /// <summary>
    /// 任务名
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 任务handler描述
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// 任务描述
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// 时间类型
    /// </summary>
    public string TimeType { get; set; } = null!;

    /// <summary>
    /// 时间表达式
    /// </summary>
    public string TimeExpression { get; set; } = null!;

    /// <summary>
    /// 失败尝试间隔
    /// </summary>
    public int AttemptInterval { get; set; }

    /// <summary>
    /// 失败尝试次数
    /// </summary>
    public int MaxAttempt { get; set; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public string JobParams { get; set; } = null!;

    /// <summary>
    /// 并发线程限制
    /// </summary>
    public int MaxThread { get; set; }

    /// <summary>
    /// 执行模式 alone: 单机 sphere:分片
    /// </summary>
    public string ExecuteMode { get; set; } = null!;

    /// <summary>
    /// 0:关闭 1:企业微信机器人
    /// </summary>
    public sbyte AlarmType { get; set; }

    /// <summary>
    /// 报警配置
    /// </summary>
    public string AlarmContent { get; set; } = null!;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 并行数
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    public long NextTriggerTime { get; set; }
}
