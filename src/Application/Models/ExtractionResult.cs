namespace IntelliImport.Application.Models;

public class ExtractionResult
{
    public string? InvoiceNo { get; set; }
    public string? Vendor { get; set; }
    public string? Date { get; set; }
    public decimal? Total { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? ValidationNote { get; set; }
    public List<ExtractionLineItem> LineItems { get; set; } = [];
}

public class ExtractionLineItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
