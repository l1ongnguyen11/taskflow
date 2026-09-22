using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class TimeLogConfiguration : IEntityTypeConfiguration<TimeLog>
{
    public void Configure(EntityTypeBuilder<TimeLog> builder)
    {
        builder.ToTable("time_log", t =>
        {
            t.HasCheckConstraint("chk_duration_positive", "duration_minutes > 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(tl => tl.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(tl => tl.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid");

        builder.Property(tl => tl.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(tl => tl.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(tl => tl.EndedAt)
            .HasColumnName("ended_at")
            .HasColumnType("timestamptz");

        builder.Property(tl => tl.DurationMinutes)
            .HasColumnName("duration_minutes")
            .HasColumnType("integer")
            .IsRequired();

        // Indexes
        builder.HasIndex(tl => new { tl.UserId, tl.StartedAt })
            .HasDatabaseName("idx_time_log_user");

        builder.HasIndex(tl => tl.TaskId)
            .HasDatabaseName("idx_time_log_task");

        // Relationships
        builder.HasOne(tl => tl.Task)
            .WithMany(t => t.TimeLogs)
            .HasForeignKey(tl => tl.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_time_log_task");

        builder.HasOne(tl => tl.User)
            .WithMany(u => u.TimeLogs)
            .HasForeignKey(tl => tl.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_time_log_user");
    }
}
