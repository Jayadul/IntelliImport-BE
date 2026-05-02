using System.Text.Json.Serialization;

namespace IntelliImport.Application.Models;

public sealed class ExtractionResult
{
    [JsonPropertyName("invoiceNo")]
    public string? InvoiceNo { get; set; }

    [JsonPropertyName("vendor")]
    public string? Vendor { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("confidenceScore")]
    public decimal ConfidenceScore { get; set; }

    [JsonPropertyName("lineItems")]
    public List<ExtractionLineItem> LineItems { get; set; } = [];
}

public sealed class ExtractionLineItem
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal { get; set; }
}
