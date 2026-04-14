using IntelliImport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliImport.Infrastructure.Persistence.Configurations;

public sealed class ExtractionJobConfiguration : IEntityTypeConfiguration<ExtractionJob>
{
    public void Configure(EntityTypeBuilder<ExtractionJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.FileName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(j => j.FileBytes)
            .HasColumnType("varbinary(max)")
            .IsRequired();

        builder.Property(j => j.CurrentChunk)
            .HasMaxLength(500);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(j => j.Status)
            .HasConversion<int>();

        // Relationship to ExtractionRecord (optional, only set after completion)
        builder.HasOne(j => j.ExtractionRecord)
            .WithMany()
            .HasForeignKey(j => j.ExtractionRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("ExtractionJobs");

        // Indexes for querying
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.CreatedAt);
    }
}