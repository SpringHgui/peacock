using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Entity.Models
{
    partial class ScUser
    {
        public static string MD5Encrypt64(string password)
        {
            string cl = $"BaoXin{password}KeJi";
            MD5 md5 = MD5.Create();
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            return Convert.ToBase64String(s);
        }
    }
}
