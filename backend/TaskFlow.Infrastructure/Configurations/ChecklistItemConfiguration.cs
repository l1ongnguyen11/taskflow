using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("checklist_item");
        builder.ConfigureBaseEntity();

        builder.Property(ci => ci.ChecklistId)
            .HasColumnName("checklist_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(ci => ci.Content)
            .HasColumnName("content")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(ci => ci.IsCompleted)
            .HasColumnName("is_completed")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ci => ci.Position)
            .HasColumnName("position")
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ci => ci.AssigneeId)
            .HasColumnName("assignee_id")
            .HasColumnType("uuid");

        // Relationships
        builder.HasOne(ci => ci.Checklist)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_checklist_item_checklist");

        builder.HasOne(ci => ci.Assignee)
            .WithMany()
            .HasForeignKey(ci => ci.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_checklist_item_assignee");
    }
}
