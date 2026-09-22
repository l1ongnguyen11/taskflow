using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permission");
        builder.ConfigureBaseEntity();

        builder.Property(p => p.Key)
            .HasColumnName("key")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // Indexes & Constraints
        builder.HasIndex(p => p.Key)
            .HasDatabaseName("uq_permission_key")
            .IsUnique();
    }
}
