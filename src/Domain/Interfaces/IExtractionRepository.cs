using IntelliImport.Domain.Entities;

namespace IntelliImport.Domain.Interfaces;

public interface IExtractionRepository
{
    Task<ExtractionRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExtractionRecord>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task AddAsync(ExtractionRecord record, CancellationToken ct = default);
    Task UpdateAsync(ExtractionRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
