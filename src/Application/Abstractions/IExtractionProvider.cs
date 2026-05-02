using IntelliImport.Application.Models;
using IntelliImport.Domain.Results;

namespace IntelliImport.Application.Abstractions;

/// <summary>
/// AI extraction provider abstraction.
/// Implement for Ollama/Phi-3, OpenAI, Azure, etc.
/// Designed to be extracted as a NuGet interface package.
/// </summary>
public interface IExtractionProvider
{
    string ProviderName { get; }

    Task<Result<ExtractionResult>> ExtractAsync(
        string pdfText,
        CancellationToken ct = default);
}
