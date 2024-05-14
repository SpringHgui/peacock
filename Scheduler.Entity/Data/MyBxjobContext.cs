using Microsoft.EntityFrameworkCore;
using Scheduler.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Entity.Data;

public partial class BxjobContext : DbContext
{
//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseMySQL("server=svrhz10-2.bx.com.cn;Port=3306;user id=root;database=bxjob;pooling=true;password=BaoXin8888;");

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScUser>().HasData(new ScUser { UserId = 1, UserName = "admin", Password = ScUser.MD5Encrypt64("XinBao0818"), CreatTime = DateTime.Now });
    }
}

