using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("checklist");
        builder.ConfigureBaseEntity();

        builder.Property(c => c.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(c => c.Position)
            .HasColumnName("position")
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(c => c.TaskId)
            .HasDatabaseName("idx_checklist_task");

        // Relationships
        builder.HasOne(c => c.Task)
            .WithMany(t => t.Checklists)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_checklist_task");
    }
}
