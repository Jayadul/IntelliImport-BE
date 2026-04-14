namespace IntelliImport.Application.Models;

/// <summary>
/// Strongly-typed model that mirrors the JSON schema enforced in the AI prompt.
/// </summary>
public sealed class ExtractionResult
{
    public string? InvoiceNo { get; set; }
    public string? Vendor { get; set; }
    public string? Date { get; set; }
    public decimal? Total { get; set; }
    public decimal ConfidenceScore { get; set; }
    public List<ExtractionLineItem> LineItems { get; set; } = new();
}

public sealed class ExtractionLineItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
