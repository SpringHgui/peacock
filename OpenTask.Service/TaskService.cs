using Scheduler.Entity.Data;
using Scheduler.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Scheduler.Service
{
    public class TaskService : BaseService
    {
        public TaskService(BxjobContext ScJobContext)
            : base(ScJobContext)
        {
        }

        public ScTask AddTask(ScTask task)
        {
            var entity = DBContext.ScTasks.Add(task);
            DBContext.SaveChanges();

            return entity.Entity;
        }

        public ScTask? GetTaskById(long taskId)
        {
            return DBContext.ScTasks.Where(x => x.TaskId == taskId).FirstOrDefault();
        }

        public IEnumerable<ScTask> LisTasks(int pageNumber, int pageSize, long? jobId, out int count)
        {
            count = DBContext.ScTasks.Where(x => jobId.HasValue ? x.JobId == jobId.Value : true).Count();

            var tasks = DBContext.ScTasks.OrderBy(b => b.JobId)
                .Where(x => jobId.HasValue ? x.JobId == jobId.Value : true).OrderByDescending(x => x.TaskId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return tasks;
        }

        public void Update(ScTask task)
        {
            DBContext.ScTasks.Update(task);
            DBContext.SaveChanges();
        }

        public void UpdateTaskFlag(long taskId, ScTaskStatus send)
        {
            var task = DBContext.ScTasks.Where(x => x.TaskId == taskId).FirstOrDefault();
            if (task == null)
            {
                throw new Exception($"实例不存在 {taskId}");
            }

            task.Flags |= (int)send;
            DBContext.SaveChanges();
        }
    }
}
