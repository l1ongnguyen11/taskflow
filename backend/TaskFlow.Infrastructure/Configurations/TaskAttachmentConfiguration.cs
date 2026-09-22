using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;
using File = TaskFlow.Domain.Entities.File;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("task_attachment");
        builder.ConfigureBaseEntity();

        builder.Property(ta => ta.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(ta => ta.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(ta => new { ta.TaskId, ta.FileId })
            .HasDatabaseName("uq_task_attachment")
            .IsUnique();

        // Relationships
        builder.HasOne(ta => ta.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(ta => ta.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_task_attachment_task");

        builder.HasOne(ta => ta.File)
            .WithMany(f => f.TaskAttachments)
            .HasForeignKey(ta => ta.FileId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_task_attachment_file");
    }
}
