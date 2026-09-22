using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitation");
        builder.ConfigureBaseEntity();

        builder.Property(i => i.WorkspaceId)
            .HasColumnName("workspace_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(i => i.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(i => i.Role)
            .HasColumnName("role")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("member");

        builder.Property(i => i.InvitedBy)
            .HasColumnName("invited_by")
            .HasColumnType("uuid");

        builder.Property(i => i.Token)
            .HasColumnName("token")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("pending");

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(i => i.Token)
            .HasDatabaseName("uq_invitation_token")
            .IsUnique();

        builder.HasIndex(i => new { i.Email, i.WorkspaceId })
            .HasDatabaseName("idx_invitation_email_workspace");

        // Relationships
        builder.HasOne(i => i.Workspace)
            .WithMany(w => w.Invitations)
            .HasForeignKey(i => i.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_invitation_workspace");

        builder.HasOne(i => i.Inviter)
            .WithMany()
            .HasForeignKey(i => i.InvitedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_invitation_invited_by");
    }
}
