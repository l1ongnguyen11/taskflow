using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("project");
        builder.ConfigureBaseEntity();

        builder.Property(p => p.WorkspaceId)
            .HasColumnName("workspace_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(p => p.Key)
            .HasColumnName("key")
            .HasColumnType("varchar(10)")
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(p => p.LeadId)
            .HasColumnName("lead_id")
            .HasColumnType("uuid");

        builder.Property(p => p.IsArchived)
            .HasColumnName("is_archived")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes & Constraints
        builder.HasIndex(p => new { p.WorkspaceId, p.Key })
            .HasDatabaseName("uq_project_key")
            .IsUnique();

        // Relationships
        builder.HasOne(p => p.Workspace)
            .WithMany(w => w.Projects)
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_project_workspace");

        builder.HasOne(p => p.Lead)
            .WithMany()
            .HasForeignKey(p => p.LeadId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_project_lead");
    }
}
