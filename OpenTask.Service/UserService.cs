using Scheduler.Entity.Data;
using Scheduler.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Service
{
    public class UserService : BaseService
    {
        public UserService(BxjobContext ScJobContext)
            : base(ScJobContext)
        {
        }

        public ScUser? GetUserByUserName(string userName)
        {
            return DBContext.ScUsers.FirstOrDefault(x => x.UserName == userName);
        }

        public bool CheckPassWord(string userName, string passWord)
        {
            var passWordHashed = ScUser.MD5Encrypt64(passWord);
            var ok = DBContext.ScUsers.Any(x => x.UserName == userName && x.Password == passWordHashed);
            return ok;
        }

    }
}
