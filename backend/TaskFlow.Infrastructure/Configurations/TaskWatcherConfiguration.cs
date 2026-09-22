using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskWatcherConfiguration : IEntityTypeConfiguration<TaskWatcher>
{
    public void Configure(EntityTypeBuilder<TaskWatcher> builder)
    {
        builder.ToTable("task_watcher");
        builder.ConfigureBaseEntity();

        builder.Property(tw => tw.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(tw => tw.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(tw => new { tw.TaskId, tw.UserId })
            .HasDatabaseName("uq_task_watcher")
            .IsUnique();

        builder.HasIndex(tw => tw.TaskId)
            .HasDatabaseName("idx_task_watcher_task");

        builder.HasIndex(tw => tw.UserId)
            .HasDatabaseName("idx_task_watcher_user");

        // Relationships
        builder.HasOne(tw => tw.Task)
            .WithMany(t => t.Watchers)
            .HasForeignKey(tw => tw.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_watcher_task");

        builder.HasOne(tw => tw.User)
            .WithMany(u => u.TaskWatchers)
            .HasForeignKey(tw => tw.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_watcher_user");
    }
}
