using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScServer
{
    public string Guid { get; set; } = null!;

    public string EndPoint { get; set; } = null!;

    public long HeartAt { get; set; }
}
