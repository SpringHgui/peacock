using Scheduler.Entity.Data;

namespace Scheduler.Service
{
    public class BaseService
    {
        protected BxjobContext DBContext;

        public BaseService(BxjobContext ScJobContext)
        {
            this.DBContext = ScJobContext;
        }
    }
}