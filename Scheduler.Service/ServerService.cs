
using Qz.Utility.Extensions;
using Scheduler.Entity.Data;
using Scheduler.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Service
{
    public class ServerService : BaseService
    {
        public ServerService(BxjobContext ScJobContext) : base(ScJobContext)
        {
        }

        public IEnumerable<ScServer> GetServerOnline(int heart)
        {
            var time = DateTime.Now.AddSeconds(-heart).ToTimestamp();
            return DBContext.ScServers.Where(x => x.HeartAt > time).ToList();
        }

        public void RegisterOrUpdate(ScServer mqttNode)
        {
            var olds = DBContext.ScServers.Where(x => x.Guid == mqttNode.Guid);
            if (!olds.Any())
            {
                DBContext.ScServers.Add(mqttNode);
            }
            else
            {
                var old = olds.FirstOrDefault();
                old.HeartAt = mqttNode.HeartAt;
                DBContext.ScServers.Update(old);
            }

            DBContext.SaveChanges();
        }

        public void UpdateSlot(IEnumerable<ScServer> nodes)
        {
            foreach (var item in nodes)
            {
                DBContext.ScServers.Update(item);
            }

            DBContext.SaveChanges();
        }
    }
}
