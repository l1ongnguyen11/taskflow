using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;

namespace TaskFlow.Infrastructure.Configurations;

public class BoardColumnConfiguration : IEntityTypeConfiguration<BoardColumn>
{
    public void Configure(EntityTypeBuilder<BoardColumn> builder)
    {
        builder.ToTable("board_column");
        builder.ConfigureBaseEntity();

        builder.Property(bc => bc.BoardId)
            .HasColumnName("board_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(bc => bc.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(bc => bc.Position)
            .HasColumnName("position")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(bc => bc.Color)
            .HasColumnName("color")
            .HasColumnType("varchar(7)");

        builder.Property(bc => bc.WipLimit)
            .HasColumnName("wip_limit")
            .HasColumnType("integer");

        builder.Property(bc => bc.IsDoneColumn)
            .HasColumnName("is_done_column")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes & Constraints
        builder.HasIndex(bc => new { bc.BoardId, bc.Position })
            .HasDatabaseName("uq_board_column_position")
            .IsUnique();

        // Relationships
        builder.HasOne(bc => bc.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(bc => bc.BoardId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_board_column_board");
    }
}
