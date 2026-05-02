namespace IntelliImport.Application.Abstractions;

public interface IJobQueue
{
    void Enqueue(Guid jobId);
    Task<Guid> DequeueAsync(CancellationToken ct);
}