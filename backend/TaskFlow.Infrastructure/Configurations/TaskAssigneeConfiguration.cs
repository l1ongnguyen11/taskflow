using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("task_assignee");
        builder.ConfigureBaseEntity();

        builder.Property(ta => ta.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(ta => ta.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(ta => new { ta.TaskId, ta.UserId })
            .HasDatabaseName("uq_task_assignee")
            .IsUnique();

        builder.HasIndex(ta => ta.UserId)
            .HasDatabaseName("idx_task_assignee_user");

        builder.HasIndex(ta => ta.TaskId)
            .HasDatabaseName("idx_task_assignee_task");

        // Relationships
        builder.HasOne(ta => ta.Task)
            .WithMany(t => t.Assignees)
            .HasForeignKey(ta => ta.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_assignee_task");

        builder.HasOne(ta => ta.User)
            .WithMany(u => u.TaskAssignees)
            .HasForeignKey(ta => ta.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_assignee_user");
    }
}
