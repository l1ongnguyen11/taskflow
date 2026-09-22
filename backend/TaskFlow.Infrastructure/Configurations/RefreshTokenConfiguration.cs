using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_token");
        builder.ConfigureBaseEntity();

        // ===== RefreshToken =====
        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(rt => rt.DeviceInfo)
            .HasColumnName("device_info")
            .HasColumnType("varchar(255)");

        builder.Property(rt => rt.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("varchar(45)");

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamptz");

        // Indexes & Constraints
        builder.HasIndex(rt => rt.TokenHash)
            .HasDatabaseName("uq_refresh_token_hash")
            .IsUnique();

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("idx_refresh_token_user");

        // Relationships
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_token_user");
    }
}