using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permission");
        builder.ConfigureBaseEntity();

        builder.Property(rp => rp.RoleId)
            .HasColumnName("role_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(rp => rp.PermissionId)
            .HasColumnName("permission_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .HasDatabaseName("uq_role_permission")
            .IsUnique();

        // Relationships
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_role_permission_role");

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_role_permission_permission");
    }
}
