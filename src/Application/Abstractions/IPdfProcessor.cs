using IntelliImport.Domain.ValueObjects;

namespace IntelliImport.Application.Abstractions;

public interface IPdfProcessor
{
    /// <summary>Extract plain text from PDF bytes.</summary>
    Result<string> ExtractText(byte[] pdfBytes);
}
