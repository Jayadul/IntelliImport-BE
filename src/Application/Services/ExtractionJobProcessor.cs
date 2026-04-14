using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Models;
using IntelliImport.Domain.Entities;
using IntelliImport.Domain.Enums;
using IntelliImport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Application.Services;

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

            // Step 1: Extract text from PDF
            logger.LogInformation("Extracting text from {File}", job.FileName);
            var textResult = pdfProcessor.ExtractText(job.FileBytes);
            
            if (textResult.IsFailure)
            {
                job.Status = ExtractionJobStatus.Failed;
                job.ErrorMessage = $"PDF extraction failed: {textResult.Error}";
                logger.LogError(job.ErrorMessage);
                return;
            }

            var pdfText = textResult.Value!;
            logger.LogInformation("Extracted {Size} chars from PDF", pdfText.Length);

            // Step 2: Chunk the text
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

            // Step 4: Process chunks sequentially
            var aggregatedResults = new List<ExtractionResult>();

            for (int i = 0; i < chunks.Count; i++)
            {
                job.CurrentChunk = $"Chunk {i + 1}/{chunks.Count}";
                job.ProcessedChunks = i;
                job.ProgressPercentage = (int)((i / (double)chunks.Count) * 100);
                
                logger.LogInformation(
                    "Processing chunk {Current}/{Total} ({Size} chars)",
                    i + 1, chunks.Count, chunks[i].Length);

                var chunkResult = await extractionProvider.ExtractAsync(chunks[i], ct);

                if (chunkResult.IsFailure)
                {
                    logger.LogWarning(
                        "Chunk {Index} extraction failed: {Error}. Continuing with other chunks.",
                        i, chunkResult.Error);
                    continue;
                }

                aggregatedResults.Add(chunkResult.Value!);
                
                // Add small delay between requests to avoid hammering Ollama
                if (i < chunks.Count - 1)
                    await Task.Delay(500, ct);
            }

            // Step 5: Aggregate results from all chunks
            logger.LogInformation("Aggregating results from {Count} chunks", aggregatedResults.Count);
            var finalResult = AggregateResults(aggregatedResults);

            // Step 6: Update extraction record
            record.InvoiceNo = finalResult.InvoiceNo;
            record.Vendor = finalResult.Vendor;
            record.TotalAmount = finalResult.Total;
            record.ConfidenceScore = finalResult.ConfidenceScore;
            record.Status = ExtractionStatus.Completed;
            record.LineItems = finalResult.LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.LineTotal
            }).ToList();

            await extractionRepository.UpdateAsync(record, ct);

            job.Status = ExtractionJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercentage = 100;

            logger.LogInformation(
                "Job {Id} completed. Invoice: {Invoice}, Vendor: {Vendor}",
                job.Id, finalResult.InvoiceNo, finalResult.Vendor);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Job {Id} was cancelled", job.Id);
            job.Status = ExtractionJobStatus.Cancelled;
            job.ErrorMessage = "Processing was cancelled";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {Id} failed with exception", job.Id);
            job.Status = ExtractionJobStatus.Failed;
            job.ErrorMessage = ex.Message;
        }
    }

    private static ExtractionResult AggregateResults(List<ExtractionResult> results)
    {
        if (results.Count == 0)
            return new ExtractionResult();

        // Take first complete invoice data
        var primary = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.InvoiceNo)) ?? results[0];

        // Aggregate line items from all chunks (remove duplicates by description)
        var allLineItems = results
            .SelectMany(r => r.LineItems)
            .DistinctBy(li => li.Description)
            .ToList();

        // Recalculate totals
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