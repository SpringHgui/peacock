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

    public virtual DbSet<ScApp> ScApps { get; set; }

    public virtual DbSet<ScJob> ScJobs { get; set; }

    public virtual DbSet<ScNode> ScNodes { get; set; }

    public virtual DbSet<ScServer> ScServers { get; set; }

    public virtual DbSet<ScTask> ScTasks { get; set; }

    public virtual DbSet<ScUser> ScUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScApp>(entity =>
        {
            entity.HasKey(e => e.Appid).HasName("PRIMARY");

            entity.ToTable("sc_app");

            entity.Property(e => e.Appid).HasColumnName("appid");
            entity.Property(e => e.AppName)
                .HasMaxLength(64)
                .HasColumnName("app_name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
        });

        modelBuilder.Entity<ScJob>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("PRIMARY");

            entity.ToTable("sc_job");

            entity.HasIndex(e => e.Name, "name");

            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.AlarmContent)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("alarm_content");
            entity.Property(e => e.AlarmType).HasColumnName("alarm_type");
            entity.Property(e => e.AttemptInterval).HasColumnName("attempt_interval");
            entity.Property(e => e.Content)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("content");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.ExecuteMode)
                .HasMaxLength(16)
                .HasDefaultValueSql("'alone'")
                .HasColumnName("execute_mode");
            entity.Property(e => e.GroupName)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasColumnName("group_name");
            entity.Property(e => e.JobParams)
                .HasMaxLength(512)
                .HasDefaultValueSql("''")
                .HasColumnName("job_params");
            entity.Property(e => e.MaxAttempt).HasColumnName("max_attempt");
            entity.Property(e => e.MaxThread).HasColumnName("max_thread");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.NextTriggerTime).HasColumnName("next_trigger_time");
            entity.Property(e => e.ThreadCount).HasColumnName("thread_count");
            entity.Property(e => e.TimeExpression)
                .HasMaxLength(64)
                .HasDefaultValueSql("''")
                .HasColumnName("time_expression");
            entity.Property(e => e.TimeType)
                .HasMaxLength(16)
                .HasDefaultValueSql("''")
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

        modelBuilder.Entity<ScServer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("sc_server");

            entity.HasIndex(e => e.Guid, "guid").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndPoint)
                .HasMaxLength(64)
                .HasColumnName("end_point");
            entity.Property(e => e.Guid)
                .HasMaxLength(64)
                .HasColumnName("guid");
            entity.Property(e => e.HeartAt).HasColumnName("heart_at");
            entity.Property(e => e.Slot)
                .HasMaxLength(64)
                .HasComment("0~16383")
                .HasColumnName("slot");
        });

        modelBuilder.Entity<ScTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PRIMARY");

            entity.ToTable("sc_task");

            entity.HasIndex(e => e.JobId, "job_id");

            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.ClientId)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasColumnName("client_id");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.Flags).HasColumnName("flags");
            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.Result)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("result");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.Status).HasColumnName("status");
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
