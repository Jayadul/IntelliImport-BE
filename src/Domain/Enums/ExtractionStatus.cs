namespace IntelliImport.Domain.Enums;

public enum ExtractionStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    DiscrepancyFound = 3,  // Math check failed
    Failed = 4
}
