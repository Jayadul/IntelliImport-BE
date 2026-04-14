using IntelliImport.Domain.Enums;

namespace IntelliImport.Domain.Entities;

public sealed class ExtractionRecord
{
    public Guid             Id              { get; init; } = Guid.NewGuid();
    public string           FileName        { get; set; } = string.Empty;
    public string           RawText         { get; set; } = string.Empty;
    public string?          RawAiResponse   { get; set; }

    // Extracted fields
    public string?          InvoiceNo       { get; set; }
    public string?          Vendor          { get; set; }
    public DateTime?        InvoiceDate     { get; set; }
    public decimal?         TotalAmount     { get; set; }
    public decimal?         ConfidenceScore { get; set; }

    // Validation
    public bool             IsLineItemSumValid { get; set; }
    public decimal?         LineItemSum     { get; set; }

    // Metadata
    public ExtractionStatus Status          { get; set; } = ExtractionStatus.Pending;
    public string?          ErrorMessage    { get; set; }
    public long             ProcessingMs    { get; set; }
    public string           ModelUsed       { get; set; } = "phi3";
    public DateTime         CreatedAt       { get; init; } = DateTime.UtcNow;
    public DateTime?        SyncedAt        { get; set; }

    public List<LineItem>   LineItems       { get; set; } = new();
}
