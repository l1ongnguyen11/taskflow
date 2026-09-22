using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("board");
        builder.ConfigureBaseEntity();

        builder.Property(b => b.ProjectId)
            .HasColumnName("project_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(b => b.ProjectId)
            .HasDatabaseName("idx_board_project")
            .HasFilter("deleted_at IS NULL");

        // Relationships
        builder.HasOne(b => b.Project)
            .WithMany(p => p.Boards)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_board_project");
    }
}
