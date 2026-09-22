using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_member");
        builder.ConfigureBaseEntity();

        builder.Property(wm => wm.WorkspaceId)
            .HasColumnName("workspace_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(wm => wm.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(wm => wm.JoinedAt)
            .HasColumnName("joined_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("now()");

        // Indexes & Constraints
        builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
            .HasDatabaseName("uq_workspace_member")
            .IsUnique();

        builder.HasIndex(wm => wm.UserId)
            .HasDatabaseName("idx_workspace_member_user");

        // Relationships
        builder.HasOne(wm => wm.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(wm => wm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_workspace_member_workspace");

        builder.HasOne(wm => wm.User)
            .WithMany(u => u.WorkspaceMemberships)
            .HasForeignKey(wm => wm.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_workspace_member_user");
    }
}
