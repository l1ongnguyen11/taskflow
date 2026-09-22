using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comment");
        builder.ConfigureBaseEntity();

        builder.Property(c => c.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid");

        builder.Property(c => c.ParentCommentId)
            .HasColumnName("parent_comment_id")
            .HasColumnType("uuid");

        builder.Property(c => c.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        // Indexes
        builder.HasIndex(c => new { c.TaskId, c.CreatedAt })
            .HasDatabaseName("idx_comment_task_created")
            .HasFilter("deleted_at IS NULL");

        // Relationships
        builder.HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_comment_task");

        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_comment_user");

        builder.HasOne(c => c.ParentComment)
            .WithMany(pc => pc.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_comment_parent");
    }
}
