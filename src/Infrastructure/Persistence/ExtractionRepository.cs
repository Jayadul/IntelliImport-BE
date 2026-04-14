using IntelliImport.Domain.Entities;
using IntelliImport.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelliImport.Infrastructure.Persistence;

public sealed class ExtractionRepository(AppDbContext db) : IExtractionRepository
{
    public async Task<ExtractionRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Extractions
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ExtractionRecord>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
        => await db.Extractions
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.LineItems)
            .ToListAsync(ct);

    public Task<int> GetTotalCountAsync(CancellationToken ct = default)
        => db.Extractions.CountAsync(ct);

    public async Task AddAsync(ExtractionRecord record, CancellationToken ct = default)
    {
        await db.Extractions.AddAsync(record, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ExtractionRecord record, CancellationToken ct = default)
    {
        db.Extractions.Update(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.Extractions.FindAsync(new object[] { id }, ct);
        if (record is null) return false;
        db.Extractions.Remove(record);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
