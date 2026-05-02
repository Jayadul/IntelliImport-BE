using IntelliImport.Application.Models;
using MediatR;

namespace IntelliImport.Application.Features.Extractions.Queries;

public sealed record GetExtractionsQuery(int Page, int PageSize) : IRequest<Result<List<ExtractionSummaryDto>>>;

public sealed class GetExtractionsQueryHandler(
    IExtractionRepository repository
) : IRequestHandler<GetExtractionsQuery, Result<List<ExtractionSummaryDto>>>
{
    public async Task<Result<List<ExtractionSummaryDto>>> Handle(
        GetExtractionsQuery request, CancellationToken ct)
    {
        var items = await repository.GetAllAsync(request.Page, request.PageSize, ct);

        var dtos = items.Select(r => new ExtractionSummaryDto(
            r.Id,
            r.FileName,
            r.InvoiceNo,
            r.Vendor,
            r.InvoiceDate,
            r.TotalAmount,
            r.ConfidenceScore,
            r.IsLineItemSumValid,
            r.Status.ToString(),
            r.ProcessingMs,
            r.CreatedAt
        )).ToList();

        return Result<List<ExtractionSummaryDto>>.Success(dtos);
    }
}
