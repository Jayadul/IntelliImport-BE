using IntelliImport.Application.Models;
using MediatR;

namespace IntelliImport.Application.Features.Extractions.Queries;

public sealed record GetExtractionsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ExtractionSummaryDto>>>;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              TotalCount,
    int              Page,
    int              PageSize,
    int              TotalPages
);

public sealed class GetExtractionsQueryHandler(IExtractionRepository repository)
    : IRequestHandler<GetExtractionsQuery, Result<PagedResult<ExtractionSummaryDto>>>
{
    public async Task<Result<PagedResult<ExtractionSummaryDto>>> Handle(
        GetExtractionsQuery request, CancellationToken ct)
    {
        var items  = await repository.GetAllAsync(request.Page, request.PageSize, ct);
        var total  = await repository.GetTotalCountAsync(ct);
        var pages  = (int)Math.Ceiling((double)total / request.PageSize);

        var dtos = items.Select(r => new ExtractionSummaryDto(
            r.Id, r.FileName, r.InvoiceNo, r.Vendor, r.InvoiceDate,
            r.TotalAmount, r.ConfidenceScore, r.IsLineItemSumValid,
            r.Status.ToString(), r.ProcessingMs, r.CreatedAt
        )).ToList();

        return Result<PagedResult<ExtractionSummaryDto>>.Success(
            new PagedResult<ExtractionSummaryDto>(dtos, total, request.Page, request.PageSize, pages));
    }
}
