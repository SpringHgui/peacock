using Microsoft.EntityFrameworkCore;
using Scheduler.Entity.Data;
using Scheduler.Entity.Models;

namespace Scheduler.Service
{
    public class JobService : BaseService
    {
        public JobService(BxjobContext ScJobContext)
            : base(ScJobContext)
        {
        }

        public ScJob AddJob(ScJob model)
        {
            model.Enabled = true;

            var ddd = DBContext.Add(model);
            DBContext.SaveChanges();

            return ddd.Entity;
        }

        public void DelJob(long jobId)
        {
            var model = DBContext.ScJobs.Where(x => x.JobId == jobId).FirstOrDefault();
            if (model != null)
            {
                DBContext.ScJobs.Remove(model);
                DBContext.SaveChanges();
            }
        }

        public async Task<bool> SwitchEnabledStatus(long jobId)
        {
            var job = DBContext.ScJobs.Where(x => x.JobId == jobId).FirstOrDefault();
            if (job == null)
            {
                throw new Exception("job不存在");
            }

            job.Enabled = !job.Enabled;
            DBContext.ScJobs.Update(job);
            var count = await DBContext.SaveChangesAsync();

            return count == 1;
        }

        public ScJob? GetJob(long jobId)
        {
            return DBContext.ScJobs.FirstOrDefault(x => x.JobId == jobId);
        }

        public IEnumerable<ScJob> ListJobs(int pageNumber, int pageSize, string? name, out int count)
        {
            count = DBContext.ScJobs.Where(x => name == null ? true : x.Name.Contains(name)).Count();

            return DBContext.ScJobs.OrderBy(b => b.JobId)
                  .Where(x => name == null ? true : x.Name.Contains(name))
                  .Skip((pageNumber - 1) * pageSize)
                  .Take(pageSize);
        }

        public void UpdateParallelCount(long jobid, int count)
        {
            // 这里并发时，可能出现数量维护不一致
            var job = GetJob(jobid);
            //job.ThreadCount += count;

            DBContext.ScJobs.Update(job);
            DBContext.SaveChangesAsync();
        }
    }
}