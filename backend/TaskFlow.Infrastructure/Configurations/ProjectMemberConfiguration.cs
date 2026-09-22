using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_member");
        builder.ConfigureBaseEntity();

        builder.Property(pm => pm.ProjectId)
            .HasColumnName("project_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(pm => pm.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(pm => pm.Role)
            .HasColumnName("role")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("member");

        // Indexes & Constraints
        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .HasDatabaseName("uq_project_member")
            .IsUnique();

        builder.HasIndex(pm => pm.UserId)
            .HasDatabaseName("idx_project_member_user");

        // Relationships
        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_project_member_project");

        builder.HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_project_member_user");
    }
}
