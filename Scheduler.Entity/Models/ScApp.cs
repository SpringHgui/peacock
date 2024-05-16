using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScApp
{
    public long Appid { get; set; }

    public string? AppName { get; set; }

    public bool? Enabled { get; set; }

    public long? CreatedBy { get; set; }

    public long? CreatedAt { get; set; }
}
