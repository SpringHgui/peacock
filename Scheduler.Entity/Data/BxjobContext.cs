using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Scheduler.Entity.Models;

namespace Scheduler.Entity.Data;

public partial class BxjobContext : DbContext
{
    public BxjobContext(DbContextOptions<BxjobContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ScJob> ScJobs { get; set; }

    public virtual DbSet<ScNode> ScNodes { get; set; }

    public virtual DbSet<ScTask> ScTasks { get; set; }

    public virtual DbSet<ScUser> ScUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScJob>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("PRIMARY");

            entity.ToTable("sc_job");

            entity.HasIndex(e => e.Name, "name");

            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.AlarmContent)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasComment("报警配置")
                .HasColumnName("alarm_content");
            entity.Property(e => e.AlarmType)
                .HasComment("0:关闭 1:企业微信机器人")
                .HasColumnName("alarm_type");
            entity.Property(e => e.AttemptInterval)
                .HasComment("失败尝试间隔")
                .HasColumnName("attempt_interval");
            entity.Property(e => e.Content)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasComment("任务handler描述")
                .HasColumnName("content");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasComment("任务描述")
                .HasColumnName("description");
            entity.Property(e => e.Enabled)
                .HasComment("是否启用")
                .HasColumnName("enabled");
            entity.Property(e => e.ExecuteMode)
                .HasMaxLength(16)
                .HasDefaultValueSql("'alone'")
                .HasComment("执行模式 alone: 单机 sphere:分片")
                .HasColumnName("execute_mode");
            entity.Property(e => e.GroupName)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasComment("分组名")
                .HasColumnName("group_name");
            entity.Property(e => e.JobParams)
                .HasMaxLength(512)
                .HasDefaultValueSql("''")
                .HasComment("任务参数")
                .HasColumnName("job_params");
            entity.Property(e => e.MaxAttempt)
                .HasComment("失败尝试次数")
                .HasColumnName("max_attempt");
            entity.Property(e => e.MaxThread)
                .HasComment("并发线程限制")
                .HasColumnName("max_thread");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasComment("任务名")
                .HasColumnName("name");
            entity.Property(e => e.NextTriggerTime)
                .HasComment("下次执行时间")
                .HasColumnName("next_trigger_time");
            entity.Property(e => e.ThreadCount)
                .HasComment("并行数")
                .HasColumnName("thread_count");
            entity.Property(e => e.TimeExpression)
                .HasMaxLength(64)
                .HasDefaultValueSql("''")
                .HasComment("时间表达式")
                .HasColumnName("time_expression");
            entity.Property(e => e.TimeType)
                .HasMaxLength(16)
                .HasDefaultValueSql("''")
                .HasComment("时间类型")
                .HasColumnName("time_type");
        });

        modelBuilder.Entity<ScNode>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("sc_node");

            entity.Property(e => e.LastHeart)
                .HasMaxLength(255)
                .HasColumnName("last_heart");
            entity.Property(e => e.NodeName)
                .HasMaxLength(255)
                .HasColumnName("node_name");
            entity.Property(e => e.Slot)
                .HasMaxLength(255)
                .HasColumnName("slot");
        });

        modelBuilder.Entity<ScTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PRIMARY");

            entity.ToTable("sc_task");

            entity.HasIndex(e => e.JobId, "job_id");

            entity.Property(e => e.TaskId)
                .HasComment("调度实例编号")
                .HasColumnName("task_id");
            entity.Property(e => e.ClientId)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasComment("执行客户端id")
                .HasColumnName("client_id");
            entity.Property(e => e.EndTime)
                .HasComment("调度结束时间")
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.Flags)
                .HasComment("flags")
                .HasColumnName("flags");
            entity.Property(e => e.JobId)
                .HasComment("任务编号")
                .HasColumnName("job_id");
            entity.Property(e => e.Result)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasComment("结果")
                .HasColumnName("result");
            entity.Property(e => e.StartTime)
                .HasComment("调度开始时间")
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasComment("状态 0:未执行 1:执行中 2:成功 3:失败")
                .HasColumnName("status");
        });

        modelBuilder.Entity<ScUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("sc_user");

            entity.HasIndex(e => e.UserName, "user_name");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatTime)
                .HasMaxLength(6)
                .HasDefaultValueSql("'0001-01-01 00:00:00.000000'")
                .HasColumnName("creat_time");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("password");
            entity.Property(e => e.UserName)
                .HasDefaultValueSql("''")
                .HasColumnName("user_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
