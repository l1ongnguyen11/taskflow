using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskLabelConfiguration : IEntityTypeConfiguration<TaskLabel>
{
    public void Configure(EntityTypeBuilder<TaskLabel> builder)
    {
        builder.ToTable("task_label");
        builder.ConfigureBaseEntity();

        builder.Property(tl => tl.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(tl => tl.LabelId)
            .HasColumnName("label_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(tl => new { tl.TaskId, tl.LabelId })
            .HasDatabaseName("uq_task_label")
            .IsUnique();

        builder.HasIndex(tl => tl.TaskId)
            .HasDatabaseName("idx_task_label_task");

        builder.HasIndex(tl => tl.LabelId)
            .HasDatabaseName("idx_task_label_label");

        // Relationships
        builder.HasOne(tl => tl.Task)
            .WithMany(t => t.TaskLabels)
            .HasForeignKey(tl => tl.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_label_task");

        builder.HasOne(tl => tl.Label)
            .WithMany(l => l.TaskLabels)
            .HasForeignKey(tl => tl.LabelId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_label_label");
    }
}
