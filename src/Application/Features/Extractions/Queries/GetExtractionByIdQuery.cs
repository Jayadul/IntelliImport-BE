using IntelliImport.Application.Models;
using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.ValueObjects;
using MediatR;

namespace IntelliImport.Application.Features.Extractions.Queries;

public sealed record GetExtractionByIdQuery(Guid Id)
    : IRequest<Result<ExtractionDetailDto>>;

public sealed class GetExtractionByIdQueryHandler(IExtractionRepository repository)
    : IRequestHandler<GetExtractionByIdQuery, Result<ExtractionDetailDto>>
{
    public async Task<Result<ExtractionDetailDto>> Handle(
        GetExtractionByIdQuery request, CancellationToken ct)
    {
        var record = await repository.GetByIdAsync(request.Id, ct);
        if (record is null)
            return Result<ExtractionDetailDto>.Failure($"Extraction {request.Id} not found.", "NOT_FOUND");

        return Result<ExtractionDetailDto>.Success(new ExtractionDetailDto(
            record.Id, record.FileName, record.InvoiceNo, record.Vendor, record.InvoiceDate,
            record.TotalAmount, record.ConfidenceScore, record.IsLineItemSumValid, record.LineItemSum,
            record.Status.ToString(), record.ErrorMessage, record.ProcessingMs, record.ModelUsed,
            record.CreatedAt, record.SyncedAt,
            record.LineItems.Select(l => new LineItemDetailDto(
                l.Description, l.Quantity, l.UnitPrice, l.LineTotal, l.IsAmountMismatch
            )).ToList()
        ));
    }
}
