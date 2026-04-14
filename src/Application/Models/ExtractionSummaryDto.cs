namespace IntelliImport.Application.Models;

public sealed record ExtractionSummaryDto(
    Guid      Id,
    string    FileName,
    string?   InvoiceNo,
    string?   Vendor,
    DateTime? InvoiceDate,
    decimal?  TotalAmount,
    decimal?  ConfidenceScore,
    bool      IsLineItemSumValid,
    string    Status,
    long      ProcessingMs,
    DateTime  CreatedAt
);

public sealed record ExtractionDetailDto(
    Guid                    Id,
    string                  FileName,
    string?                 InvoiceNo,
    string?                 Vendor,
    DateTime?               InvoiceDate,
    decimal?                TotalAmount,
    decimal?                ConfidenceScore,
    bool                    IsLineItemSumValid,
    decimal?                LineItemSum,
    string                  Status,
    string?                 ErrorMessage,
    long                    ProcessingMs,
    string                  ModelUsed,
    DateTime                CreatedAt,
    DateTime?               SyncedAt,
    List<LineItemDetailDto> LineItems
);

public sealed record LineItemDetailDto(
    string  Description,
    int     Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool    IsAmountMismatch
);
