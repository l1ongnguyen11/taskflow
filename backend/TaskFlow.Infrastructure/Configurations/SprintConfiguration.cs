using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("sprint");
        builder.ConfigureBaseEntity();

        builder.Property(s => s.ProjectId)
            .HasColumnName("project_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(s => s.Goal)
            .HasColumnName("goal")
            .HasColumnType("text");

        builder.Property(s => s.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date");

        builder.Property(s => s.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("planning");

        // Indexes & Constraints
        builder.HasIndex(s => s.ProjectId)
            .HasDatabaseName("uq_sprint_active_per_project")
            .IsUnique()
            .HasFilter("status = 'active' AND deleted_at IS NULL");

        // Relationships
        builder.HasOne(s => s.Project)
            .WithMany(p => p.Sprints)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_sprint_project");
    }
}
