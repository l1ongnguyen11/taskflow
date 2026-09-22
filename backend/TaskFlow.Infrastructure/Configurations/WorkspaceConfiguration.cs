using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspace");
        builder.ConfigureBaseEntity();

        builder.Property(w => w.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(w => w.Slug)
            .HasColumnName("slug")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.HasIndex(w => w.Slug)
            .HasDatabaseName("uq_workspace_slug")
            .IsUnique();

        builder.Property(w => w.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(w => w.LogoUrl)
            .HasColumnName("logo_url")
            .HasColumnType("text");

        builder.Property(w => w.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid");

        // Relationships
        builder.HasOne(w => w.Creator)
            .WithMany()
            .HasForeignKey(w => w.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_workspace_created_by");
    }
}
