using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Extensions;
using File = TaskFlow.Domain.Entities.File;

namespace TaskFlow.Infrastructure.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<File>
{
    public void Configure(EntityTypeBuilder<File> builder)
    {
        builder.ToTable("file");
        builder.ConfigureBaseEntity();

        builder.Property(f => f.OriginalName)
            .HasColumnName("original_name")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(f => f.StoredPath)
            .HasColumnName("stored_path")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(f => f.MimeType)
            .HasColumnName("mime_type")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(f => f.SizeBytes)
            .HasColumnName("size_bytes")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(f => f.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasColumnType("uuid");

        // Indexes
        builder.HasIndex(f => f.UploadedBy)
            .HasDatabaseName("idx_file_uploaded_by")
            .HasFilter("deleted_at IS NULL");

        // Relationships
        builder.HasOne(f => f.Uploader)
            .WithMany()
            .HasForeignKey(f => f.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_file_uploaded_by");
    }
}
