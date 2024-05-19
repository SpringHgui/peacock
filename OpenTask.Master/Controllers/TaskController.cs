using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Scheduler.Master.Models;
using Scheduler.Service;

namespace Scheduler.Master.Controllers
{
    public class TaskController : BaseApiController
    {
        TaskService taskService;
        public TaskController(TaskService taskService)
        {
            this.taskService = taskService;
        }

        [HttpGet]
        public ResultData LisTasks(int pageNumber, int pageSize, long? jobId)
        {
            var list = taskService.LisTasks(pageNumber, pageSize, jobId, out int count);

            ResultData.data = new
            {
                rows = list,
                count
            };

            return ResultData;
        }
    }
}
