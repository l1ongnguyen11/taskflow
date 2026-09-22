using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");
        builder.ConfigureBaseEntity();

    
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("uq_user_email")
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasColumnType("text");

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(true);
    }
}
