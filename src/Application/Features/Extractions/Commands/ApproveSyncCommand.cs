using IntelliImport.Domain.Enums;
using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Application.Features.Extractions.Commands;

public sealed record ApproveSyncCommand(Guid ExtractionId) : IRequest<Result>;

public sealed class ApproveSyncCommandHandler(
    IExtractionRepository repository,
    ILogger<ApproveSyncCommandHandler> logger
) : IRequestHandler<ApproveSyncCommand, Result>
{
    public async Task<Result> Handle(ApproveSyncCommand request, CancellationToken ct)
    {
        var record = await repository.GetByIdAsync(request.ExtractionId, ct);
        if (record is null)
            return Result.Failure(
                $"Extraction {request.ExtractionId} not found.",
                "NOT_FOUND");

        if (record.Status == ExtractionStatus.Failed)
            return Result.Failure(
                "Cannot sync a failed extraction.",
                "INVALID_STATE");

        logger.LogInformation(
            "Simulating ERP sync for extraction {Id} ({InvoiceNo})",
            record.Id, record.InvoiceNo);

        // Simulate ERP call
        await Task.Delay(500, ct);

        record.Status = ExtractionStatus.Completed;
        record.SyncedAt = DateTime.UtcNow;
        await repository.UpdateAsync(record, ct);

        logger.LogInformation("ERP sync complete for {Id}", record.Id);
        return Result.Success();
    }
}
