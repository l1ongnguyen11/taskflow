using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notification");
        builder.ConfigureBaseEntity();

        builder.Property(n => n.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(n => n.WorkspaceId)
            .HasColumnName("workspace_id")
            .HasColumnType("uuid");

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(n => n.Body)
            .HasColumnName("body")
            .HasColumnType("text");

        builder.Property(n => n.EntityType)
            .HasColumnName("entity_type")
            .HasColumnType("varchar(50)");

        builder.Property(n => n.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("uuid");

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("idx_notification_user_unread")
            .IsDescending(false, false, true);

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notification_user");

        builder.HasOne(n => n.Workspace)
            .WithMany(w => w.Notifications)
            .HasForeignKey(n => n.WorkspaceId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_notification_workspace");
    }
}
