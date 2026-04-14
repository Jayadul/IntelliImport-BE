using IntelliImport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliImport.Infrastructure.Persistence.Configurations;

public sealed class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    public void Configure(EntityTypeBuilder<LineItem> b)
    {
        b.ToTable("LineItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).IsRequired().HasMaxLength(500);
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");
        b.Ignore(x => x.IsAmountMismatch); // computed, not stored
    }
}
