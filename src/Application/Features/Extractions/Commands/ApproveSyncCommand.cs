using IntelliImport.Domain.Enums;
using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

// Alias avoids collision between our non-generic Result and MediatR internals
using CommandResult = IntelliImport.Domain.ValueObjects.Result;

namespace IntelliImport.Application.Features.Extractions.Commands;

public sealed record ApproveSyncCommand(Guid ExtractionId) : IRequest<CommandResult>;

public sealed class ApproveSyncCommandHandler(
    IExtractionRepository              repository,
    ILogger<ApproveSyncCommandHandler> logger
) : IRequestHandler<ApproveSyncCommand, CommandResult>
{
    public async Task<CommandResult> Handle(ApproveSyncCommand request, CancellationToken ct)
    {
        var record = await repository.GetByIdAsync(request.ExtractionId, ct);
        if (record is null)
            return CommandResult.Failure(
                $"Extraction {request.ExtractionId} not found.", "NOT_FOUND");

        if (record.Status == ExtractionStatus.Failed)
            return CommandResult.Failure("Cannot sync a failed extraction.", "INVALID_STATE");

        logger.LogInformation(
            "Simulating ERP sync for extraction {Id} ({InvoiceNo})", record.Id, record.InvoiceNo);

        await Task.Delay(500, ct); // simulated ERP call

        record.Status   = ExtractionStatus.Validated;
        record.SyncedAt = DateTime.UtcNow;
        await repository.UpdateAsync(record, ct);

        logger.LogInformation("ERP sync complete for {Id}", record.Id);
        return CommandResult.Ok;
    }
}
