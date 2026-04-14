using IntelliImport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliImport.Infrastructure.Persistence.Configurations;

public sealed class ExtractionRecordConfiguration : IEntityTypeConfiguration<ExtractionRecord>
{
    public void Configure(EntityTypeBuilder<ExtractionRecord> b)
    {
        b.ToTable("ExtractionRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(512);
        b.Property(x => x.RawText).HasColumnType("nvarchar(max)");
        b.Property(x => x.RawAiResponse).HasColumnType("nvarchar(max)");
        b.Property(x => x.InvoiceNo).HasMaxLength(100);
        b.Property(x => x.Vendor).HasMaxLength(300);
        b.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
        b.Property(x => x.ConfidenceScore).HasColumnType("decimal(5,4)");
        b.Property(x => x.LineItemSum).HasColumnType("decimal(18,4)");
        b.Property(x => x.ModelUsed).HasMaxLength(100);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);

        b.HasMany(x => x.LineItems)
         .WithOne()
         .HasForeignKey(l => l.ExtractionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.Status);
    }
}
