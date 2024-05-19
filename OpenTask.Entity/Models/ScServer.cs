using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScServer
{
    public long Id { get; set; }

    public string Guid { get; set; } = null!;

    public string EndPoint { get; set; } = null!;

    public long HeartAt { get; set; }

    /// <summary>
    /// 0~16383
    /// </summary>
    public string? Slot { get; set; }
}
