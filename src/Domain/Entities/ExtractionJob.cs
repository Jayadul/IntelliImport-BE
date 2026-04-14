using IntelliImport.Domain.Enums;

namespace IntelliImport.Domain.Entities;

public sealed class ExtractionJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public byte[] FileBytes { get; set; } = [];
    
    // Job tracking
    public ExtractionJobStatus Status { get; set; } = ExtractionJobStatus.Queued;
    public int ProgressPercentage { get; set; }
    public string? CurrentChunk { get; set; }
    public int TotalChunks { get; set; }
    public int ProcessedChunks { get; set; }
    
    // Results
    public Guid? ExtractionRecordId { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Timing
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public ExtractionRecord? ExtractionRecord { get; set; }
}