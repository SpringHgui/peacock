using System;
using System.Collections.Generic;

namespace Scheduler.Entity.Models;

public partial class ScUser
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime CreatTime { get; set; }
}
