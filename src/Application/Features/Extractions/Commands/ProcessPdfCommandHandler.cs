using System.Diagnostics;
using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Models;
using IntelliImport.Domain.Entities;
using IntelliImport.Domain.Enums;
using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Application.Features.Extractions.Commands;

public sealed class ProcessPdfCommandHandler(
    IExtractionRepository repository,
    IPdfProcessor         pdfProcessor,
    IExtractionProvider   extractionProvider,
    ILogger<ProcessPdfCommandHandler> logger
) : IRequestHandler<ProcessPdfCommand, Result<ExtractionDetailDto>>
{
    public async Task<Result<ExtractionDetailDto>> Handle(
        ProcessPdfCommand request, CancellationToken ct)
    {
        var sw     = Stopwatch.StartNew();
        var record = new ExtractionRecord
        {
            FileName = request.FileName,
            Status   = ExtractionStatus.Processing
        };

        await repository.AddAsync(record, ct);

        try
        {
            // 1. Extract PDF text
            var textResult = pdfProcessor.ExtractText(request.FileBytes);
            if (textResult.IsFailure)
            {
                record.Status       = ExtractionStatus.Failed;
                record.ErrorMessage = textResult.Error;
                await repository.UpdateAsync(record, ct);
                return Result<ExtractionDetailDto>.Failure(textResult.Error!, "PDF_EXTRACTION_FAILED");
            }

            record.RawText = textResult.Value!;

            // 2. Run AI extraction with Polly-backed provider
            logger.LogInformation("Running AI extraction on {File} via {Provider}",
                request.FileName, extractionProvider.ProviderName);

            var aiResult = await extractionProvider.ExtractAsync(record.RawText, ct);
            if (aiResult.IsFailure)
            {
                record.Status       = ExtractionStatus.Failed;
                record.ErrorMessage = aiResult.Error;
                await repository.UpdateAsync(record, ct);
                return Result<ExtractionDetailDto>.Failure(aiResult.Error!, "AI_EXTRACTION_FAILED");
            }

            var extracted = aiResult.Value!;

            // 3. Map to entity
            record.InvoiceNo       = extracted.InvoiceNo;
            record.Vendor          = extracted.Vendor;
            record.TotalAmount     = extracted.Total;
            record.ConfidenceScore = extracted.ConfidenceScore;
            record.ModelUsed       = extractionProvider.ProviderName;

            if (DateTime.TryParse(extracted.Date, out var parsedDate))
                record.InvoiceDate = parsedDate;

            record.LineItems = extracted.LineItems.Select(l => new LineItem
            {
                ExtractionId = record.Id,
                Description  = l.Description,
                Quantity     = l.Quantity,
                UnitPrice    = l.UnitPrice,
                LineTotal    = l.LineTotal
            }).ToList();

            // 4. Validate: sum(LineItems) == TotalAmount
            record.LineItemSum        = record.LineItems.Sum(l => l.LineTotal);
            record.IsLineItemSumValid = record.TotalAmount.HasValue &&
                                       Math.Abs(record.LineItemSum.Value - record.TotalAmount.Value) < 0.01m;

            sw.Stop();
            record.ProcessingMs = sw.ElapsedMilliseconds;
            record.Status       = ExtractionStatus.Completed;

            await repository.UpdateAsync(record, ct);

            logger.LogInformation(
                "Extraction {Id} complete in {Ms}ms. Valid={Valid}, Confidence={Conf:F2}",
                record.Id, record.ProcessingMs, record.IsLineItemSumValid, record.ConfidenceScore);

            return Result<ExtractionDetailDto>.Success(MapToDetail(record));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Extraction failed for {File}", request.FileName);
            record.Status       = ExtractionStatus.Failed;
            record.ErrorMessage = ex.Message;
            await repository.UpdateAsync(record, ct);
            return Result<ExtractionDetailDto>.Failure(ex.Message, "UNHANDLED_ERROR");
        }
    }

    private static ExtractionDetailDto MapToDetail(ExtractionRecord r) => new(
        r.Id, r.FileName, r.InvoiceNo, r.Vendor, r.InvoiceDate,
        r.TotalAmount, r.ConfidenceScore, r.IsLineItemSumValid, r.LineItemSum,
        r.Status.ToString(), r.ErrorMessage, r.ProcessingMs, r.ModelUsed,
        r.CreatedAt, r.SyncedAt,
        r.LineItems.Select(l => new LineItemDetailDto(
            l.Description, l.Quantity, l.UnitPrice, l.LineTotal, l.IsAmountMismatch
        )).ToList()
    );
}
