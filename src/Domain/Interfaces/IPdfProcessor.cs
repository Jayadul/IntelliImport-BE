using IntelliImport.Domain.Results;

namespace IntelliImport.Domain.Interfaces;

public interface IPdfProcessor
{
    Result<string> ExtractText(byte[] fileBytes);
}