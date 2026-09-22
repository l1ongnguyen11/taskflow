using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activity");
        builder.ConfigureBaseEntity();

        builder.Property(a => a.WorkspaceId)
            .HasColumnName("workspace_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.ActorId)
            .HasColumnName("actor_id")
            .HasColumnType("uuid");

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(a => a.OldValue)
            .HasColumnName("old_value")
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValue)
            .HasColumnName("new_value")
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt })
            .HasDatabaseName("idx_activity_entity")
            .IsDescending(false, false, true);

        builder.HasIndex(a => new { a.WorkspaceId, a.CreatedAt })
            .HasDatabaseName("idx_activity_workspace")
            .IsDescending(false, true);

        // Relationships
        builder.HasOne(a => a.Workspace)
            .WithMany(w => w.Activities)
            .HasForeignKey(a => a.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_activity_workspace");

        builder.HasOne(a => a.Actor)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_activity_actor");
    }
}
