using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScTask
{
    public long TaskId { get; set; }

    public long JobId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string Result { get; set; } = null!;

    public int Flags { get; set; }

    public sbyte Status { get; set; }

    public string ClientId { get; set; } = null!;
}
