using System.ComponentModel.DataAnnotations;

namespace Scheduler.Master.Models
{
    public class GetJobListRequest
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 15;

        public string? name { get; set; }
    }
}
