using IntelliImport.Application.Features.Extractions.Commands;
using IntelliImport.Application.Features.Extractions.Queries;
using IntelliImport.Domain.Entities;
using IntelliImport.Infrastructure.Persistence;
using System.Threading.Channels;

namespace IntelliImport.API.Controllers;

/// <summary>
/// Invoice extraction API with anti-timeout asynchronous pipeline.
/// All extraction operations are queued and processed in the background.
/// </summary>
[ApiController]
[Route("api/extractions")]
[Produces("application/json")]
public sealed class ExtractionController(
    ChannelWriter<Guid> extractionQueue,
    ISender mediator,
    AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Upload PDF for extraction (returns 202 immediately).
    /// The PDF is queued for background processing - no timeout risk.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only PDF files accepted" });

        // Read file
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        // Create job record
        var job = new ExtractionJob
        {
            FileName = file.FileName,
            FileBytes = ms.ToArray()
        };

        dbContext.ExtractionJobs.Add(job);
        await dbContext.SaveChangesAsync(ct);

        // Queue for processing
        await extractionQueue.WriteAsync(job.Id, ct);

        // Return 202 Accepted immediately
        return Accepted($"api/extractions/jobs/{job.Id}", new
        {
            jobId = job.Id,
            status = "queued",
            message = "Processing started. Poll status endpoint for progress.",
            statusUrl = $"api/extractions/jobs/{job.Id}"
        });
    }

    /// <summary>
    /// Get job status and processing progress.
    /// </summary>
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
            progress = new
            {
                percentage = job.ProgressPercentage,
                currentChunk = job.CurrentChunk,
                processedChunks = job.ProcessedChunks,
                totalChunks = job.TotalChunks
            },
            extractionId = job.ExtractionRecordId,
            error = job.ErrorMessage,
            timing = new
            {
                createdAt = job.CreatedAt,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt
            }
        });
    }

    /// <summary>
    /// Get extraction results (once job is completed).
    /// </summary>
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

    /// <summary>
    /// Approve extraction and simulate ERP sync.
    /// </summary>
    [HttpPost("{id:guid}/approve-sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveSync(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveSyncCommand(id), ct);

        return result.IsSuccess
            ? Ok(new { message = "Successfully synced to ERP.", extractionId = id })
            : BadRequest(new { error = result.Error, code = result.Error });
    }

    /// <summary>
    /// Delete an extraction record and its line items.
    /// </summary>
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

    /// <summary>
    /// Get paginated extraction history.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetExtractionsQuery(page, Math.Min(pageSize, 100)), ct);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
