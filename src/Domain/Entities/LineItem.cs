namespace IntelliImport.Domain.Entities;

public sealed class LineItem
{
    public Guid   Id          { get; init; } = Guid.NewGuid();
    public Guid   ExtractionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int    Quantity    { get; set; }
    public decimal UnitPrice  { get; set; }
    public decimal LineTotal  { get; set; }

    // Computed validation flag — not persisted
    public bool IsAmountMismatch => Math.Abs(LineTotal - (Quantity * UnitPrice)) > 0.01m;
}
