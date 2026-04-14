using IntelliImport.Application.Features.Extractions.Commands;
using IntelliImport.Application.Features.Extractions.Queries;
using IntelliImport.Domain.Entities;
using IntelliImport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IntelliImport.API.Controllers;

[ApiController]
[Route("api/extractions")]
[Produces("application/json")]
public sealed class ExtractionController(
    ISender mediator,
    AppDbContext dbContext) : ControllerBase
{
    /// <summary>Upload PDF for processing (returns immediately with job ID).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only PDF files are accepted." });

        // Read file into memory
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        // Create job (don't process yet)
        var job = new ExtractionJob
        {
            FileName = file.FileName,
            FileBytes = ms.ToArray()
        };

        dbContext.ExtractionJobs.Add(job);
        await dbContext.SaveChangesAsync(ct);

        return Accepted($"api/extractions/jobs/{job.Id}", new
        {
            jobId = job.Id,
            status = "queued",
            message = "Processing started. Poll this endpoint for status."
        });
    }

    /// <summary>Get job status and progress.</summary>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken ct)
    {
        var job = await dbContext.ExtractionJobs.FindAsync(
            new object[] { jobId }, cancellationToken: ct);

        if (job is null)
            return NotFound();

        return Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            progress = job.ProgressPercentage,
            currentChunk = job.CurrentChunk,
            processedChunks = job.ProcessedChunks,
            totalChunks = job.TotalChunks,
            extractionId = job.ExtractionRecordId,
            error = job.ErrorMessage,
            createdAt = job.CreatedAt,
            completedAt = job.CompletedAt
        });
    }

    /// <summary>Get extraction results (once job is completed).</summary>
    [HttpGet("{extractionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExtraction(Guid extractionId, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetExtractionByIdQuery(extractionId), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }

    /// <summary>Approve extraction and simulate ERP sync.</summary>
    [HttpPost("{id:guid}/approve-sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveSync(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveSyncCommand(id), ct);

        return result.IsSuccess
            ? Ok(new { message = "Successfully synced to ERP.", extractionId = id })
            : BadRequest(new { error = result.Error, code = result.ErrorCode });
    }

    /// <summary>Delete an extraction record and its line items.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteExtractionCommand(id), ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
