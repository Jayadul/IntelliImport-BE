using IntelliImport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntelliImport.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ExtractionJob> ExtractionJobs { get; set; } = default!;
    public DbSet<ExtractionRecord> Extractions { get; set; } = default!;
    public DbSet<LineItem> LineItems { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(mb);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        => base.SaveChangesAsync(ct);
}
