using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("task_dependency", t =>
        {
            t.HasCheckConstraint("chk_no_self_dependency", "task_id != depends_on_id");
        });
        builder.ConfigureBaseEntity();

        builder.Property(td => td.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(td => td.DependsOnId)
            .HasColumnName("depends_on_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(td => td.Type)
            .HasColumnName("type")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("finish_to_start");

        // Indexes & Constraints
        builder.HasIndex(td => new { td.TaskId, td.DependsOnId })
            .HasDatabaseName("uq_task_dependency")
            .IsUnique();

        builder.HasIndex(td => td.TaskId)
            .HasDatabaseName("idx_task_dependency_task");

        builder.HasIndex(td => td.DependsOnId)
            .HasDatabaseName("idx_task_dependency_depends_on");

        // Relationships
        builder.HasOne(td => td.Task)
            .WithMany(t => t.Dependencies)
            .HasForeignKey(td => td.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_dependency_task");

        builder.HasOne(td => td.DependsOn)
            .WithMany(t => t.Dependents)
            .HasForeignKey(td => td.DependsOnId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_dependency_depends_on");
    }
}
