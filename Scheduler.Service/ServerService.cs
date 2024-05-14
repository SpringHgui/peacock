
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
            Console.Write(time);
            return DBContext.ScServers.Where(x => x.HeartAt > time).ToList();
        }

        public void RegisterOrUpdate(ScServer mqttNode)
        {
            var old = DBContext.ScServers.Where(x => x.Guid == mqttNode.Guid);
            if (!old.Any())
            {
                DBContext.ScServers.Add(mqttNode);
            }
            else
            {
                DBContext.ScServers.Update(mqttNode);
            }

            DBContext.SaveChanges();
        }
    }
}
