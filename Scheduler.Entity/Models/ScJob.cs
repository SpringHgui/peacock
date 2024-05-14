using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScJob
{
    public long JobId { get; set; }

    public string GroupName { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string TimeType { get; set; } = null!;

    public string TimeExpression { get; set; } = null!;

    public int AttemptInterval { get; set; }

    public int MaxAttempt { get; set; }

    public string JobParams { get; set; } = null!;

    public int MaxThread { get; set; }

    public string ExecuteMode { get; set; } = null!;

    public sbyte AlarmType { get; set; }

    public string AlarmContent { get; set; } = null!;

    public bool Enabled { get; set; }

    public int ThreadCount { get; set; }

    public long NextTriggerTime { get; set; }
}
