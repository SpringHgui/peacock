using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScNode
{
    public string? NodeName { get; set; }

    public string? LastHeart { get; set; }

    public string? Slot { get; set; }
}
