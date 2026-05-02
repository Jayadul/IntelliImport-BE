using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Models;
using IntelliImport.Domain.Entities;
using IntelliImport.Domain.Enums;
using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.Results;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Application.Services;

/// <summary>
/// Business logic for invoice extraction with validation and discrepancy detection.
/// Implements math checks and business rules.
/// </summary>
public sealed class ExtractionJobProcessor(
    IExtractionRepository extractionRepository,
    IPdfProcessor pdfProcessor,
    IExtractionProvider extractionProvider,
    ILogger<ExtractionJobProcessor> logger)
{
    public async Task ProcessJobAsync(ExtractionJob job, CancellationToken ct = default)
    {
        try
        {
            job.Status = ExtractionJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;

            // Step 1: Extract PDF text
            logger.LogInformation("Extracting text from {File}", job.FileName);
            var textResult = pdfProcessor.ExtractText(job.FileBytes);
            
            if (!textResult.IsSuccess)
            {
                job.Status = ExtractionJobStatus.Failed;
                job.ErrorMessage = $"PDF extraction failed: {textResult.Error}";
                return;
            }

            var pdfText = textResult.Value ?? string.Empty;
            logger.LogInformation("Extracted {Size} chars from PDF", pdfText.Length);

            // Step 2: Chunk text intelligently
            var chunks = PdfChunkingService.ChunkPdfText(pdfText);
            job.TotalChunks = chunks.Count;
            logger.LogInformation("Split PDF into {Count} chunks", chunks.Count);

            // Step 3: Create extraction record
            var record = new ExtractionRecord
            {
                FileName = job.FileName,
                RawText = pdfText,
                Status = ExtractionStatus.Processing,
                ModelUsed = extractionProvider.ProviderName
            };
            await extractionRepository.AddAsync(record, ct);
            job.ExtractionRecordId = record.Id;

            // Step 4: Process chunks
            var aggregatedResults = new List<ExtractionResult>();

            for (int i = 0; i < chunks.Count; i++)
            {
                job.CurrentChunk = $"Chunk {i + 1}/{chunks.Count}";
                job.ProcessedChunks = i;
                job.ProgressPercentage = (int)((i / (double)chunks.Count) * 100);

                logger.LogInformation("Processing chunk {Current}/{Total}",
                    i + 1, chunks.Count);

                var chunkResult = await extractionProvider.ExtractAsync(chunks[i], ct);

                if (!chunkResult.IsSuccess)
                {
                    logger.LogWarning("Chunk {Index} extraction failed: {Error}", i, chunkResult.Error);
                    continue;
                }

                if (chunkResult.Value != null)
                {
                    aggregatedResults.Add(chunkResult.Value);
                }

                if (i < chunks.Count - 1)
                    await Task.Delay(500, ct);
            }

            // Step 5: Aggregate and validate
            logger.LogInformation("Aggregating {Count} chunk results", aggregatedResults.Count);
            var finalResult = AggregateResults(aggregatedResults);

            // Step 6: Apply business rules (discrepancy detection)
            var lineItemSum = finalResult.LineItems.Sum(li => li.LineTotal);
            var hasDiscrepancy = finalResult.Total.HasValue && 
                Math.Abs(lineItemSum - finalResult.Total.Value) > 0.01m;

            if (hasDiscrepancy)
            {
                job.ErrorMessage = $"Discrepancy detected: Sum of line items ({lineItemSum:C}) " +
                    $"does not match Total ({finalResult.Total:C})";
                logger.LogWarning(job.ErrorMessage);
            }

            // Step 7: Reload and update fresh record
            var freshRecord = await extractionRepository.GetByIdAsync(record.Id, ct);
            if (freshRecord is null)
            {
                job.Status = ExtractionJobStatus.Failed;
                job.ErrorMessage = "Record was deleted during processing";
                return;
            }

            freshRecord.InvoiceNo = finalResult.InvoiceNo;
            freshRecord.Vendor = finalResult.Vendor;
            freshRecord.TotalAmount = finalResult.Total;
            freshRecord.ConfidenceScore = finalResult.ConfidenceScore;
            freshRecord.Status = hasDiscrepancy 
                ? ExtractionStatus.DiscrepancyFound 
                : ExtractionStatus.Completed;
            freshRecord.ErrorMessage = job.ErrorMessage;
            freshRecord.LineItems = finalResult.LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.LineTotal
            }).ToList();

            await extractionRepository.UpdateAsync(freshRecord, ct);

            job.Status = ExtractionJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercentage = 100;

            logger.LogInformation(
                "Job {Id} completed: {Invoice} from {Vendor}",
                job.Id, finalResult.InvoiceNo ?? "unknown", finalResult.Vendor ?? "unknown");
        }
        catch (OperationCanceledException)
        {
            job.Status = ExtractionJobStatus.Cancelled;
            job.ErrorMessage = "Processing was cancelled";
            logger.LogWarning("Job {Id} was cancelled", job.Id);
        }
        catch (Exception ex)
        {
            job.Status = ExtractionJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            logger.LogError(ex, "Job {Id} failed", job.Id);
        }
    }

    private static ExtractionResult AggregateResults(List<ExtractionResult> results)
    {
        if (results.Count == 0)
            return new ExtractionResult();

        var primary = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.InvoiceNo)) ?? results[0];

        var allLineItems = results
            .SelectMany(r => r.LineItems)
            .DistinctBy(li => li.Description)
            .ToList();

        var lineItemSum = allLineItems.Sum(li => li.LineTotal);

        return new ExtractionResult
        {
            InvoiceNo = primary.InvoiceNo,
            Vendor = primary.Vendor,
            Date = primary.Date,
            Total = primary.Total ?? lineItemSum,
            ConfidenceScore = results.Average(r => r.ConfidenceScore),
            LineItems = allLineItems
        };
    }
}