using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("task");
        builder.ConfigureBaseEntity();

        builder.Property(t => t.BoardColumnId)
            .HasColumnName("board_column_id")
            .HasColumnType("uuid");

        builder.Property(t => t.ParentTaskId)
            .HasColumnName("parent_task_id")
            .HasColumnType("uuid");

        builder.Property(t => t.SprintId)
            .HasColumnName("sprint_id")
            .HasColumnType("uuid");

        builder.Property(t => t.ReporterId)
            .HasColumnName("reporter_id")
            .HasColumnType("uuid");

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("task");

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("medium");

        builder.Property(t => t.TaskNumber)
            .HasColumnName("task_number")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(t => t.Position)
            .HasColumnName("position")
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.StoryPoints)
            .HasColumnName("story_points")
            .HasColumnType("smallint");

        builder.Property(t => t.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date");

        builder.Property(t => t.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date");

        builder.Property(t => t.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(t => new { t.BoardColumnId, t.Position })
            .HasDatabaseName("idx_task_board_column_position")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(t => t.SprintId)
            .HasDatabaseName("idx_task_sprint")
            .HasFilter("sprint_id IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(t => t.DueDate)
            .HasDatabaseName("idx_task_due_date")
            .HasFilter("due_date IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(t => t.ParentTaskId)
            .HasDatabaseName("idx_task_parent")
            .HasFilter("parent_task_id IS NOT NULL");

        builder.HasIndex(t => t.ReporterId)
            .HasDatabaseName("idx_task_reporter")
            .HasFilter("deleted_at IS NULL");

        // Relationships
        builder.HasOne(t => t.BoardColumn)
            .WithMany(bc => bc.Tasks)
            .HasForeignKey(t => t.BoardColumnId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_task_board_column");

        builder.HasOne(t => t.ParentTask)
            .WithMany(pt => pt.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_parent");

        builder.HasOne(t => t.Sprint)
            .WithMany(s => s.Tasks)
            .HasForeignKey(t => t.SprintId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_task_sprint");

        builder.HasOne(t => t.Reporter)
            .WithMany()
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_task_reporter");
    }
}
