using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("label");
        builder.ConfigureBaseEntity();

        builder.Property(l => l.WorkspaceId)
            .HasColumnName("workspace_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(l => l.Color)
            .HasColumnName("color")
            .HasColumnType("varchar(7)")
            .IsRequired();

        // Indexes & Constraints
        builder.HasIndex(l => new { l.WorkspaceId, l.Name })
            .HasDatabaseName("uq_label_name")
            .IsUnique();

        builder.HasIndex(l => l.WorkspaceId)
            .HasDatabaseName("idx_label_workspace")
            .HasFilter("deleted_at IS NULL");

        // Relationships
        builder.HasOne(l => l.Workspace)
            .WithMany(w => w.Labels)
            .HasForeignKey(l => l.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_label_workspace");
    }
}
