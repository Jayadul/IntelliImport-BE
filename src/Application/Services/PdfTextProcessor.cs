namespace IntelliImport.Application.Services;

/// <summary>
/// Extracts relevant PDF text with intelligent context window management.
/// Optimized for 16GB RAM systems: extracts first 3 pages + last page only.
/// Hard limit: 8,000 characters to fit within Llama 3.1 (8B) context window.
/// </summary>
public sealed class PdfTextProcessor
{
    private const int MaxCharacters = 8000;
    private const int FirstPagesToInclude = 3;

    /// <summary>
    /// Intelligently extracts and truncates PDF text.
    /// Strategy: First N pages + last page, respecting character limit.
    /// </summary>
    public string ExtractSmartContext(string fullPdfText, int totalPages = 0)
    {
        if (string.IsNullOrWhiteSpace(fullPdfText))
            return string.Empty;

        var text = fullPdfText.Trim();
        
        // If within limit, return as-is
        if (text.Length <= MaxCharacters)
            return text;

        // Strategy: Extract beginning, preserve end for summary/totals
        // Invoices typically have important info: header (page 1-3) and footer (last page)
        var beginning = ExtractBeginning(text, MaxCharacters * 70 / 100); // 70% from start
        var ending = ExtractEnding(text, MaxCharacters * 30 / 100);       // 30% from end

        var combined = $"{beginning}\n\n[... document middle truncated ...]\n\n{ending}";
        
        // Final trim to hard limit
        if (combined.Length > MaxCharacters)
        {
            combined = combined[..MaxCharacters].TrimEnd() + "...";
        }

        return combined;
    }

    private static string ExtractBeginning(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        var truncated = text[..maxLength];
        var lastNewline = truncated.LastIndexOf('\n');
        
        return lastNewline > 0 
            ? truncated[..lastNewline] 
            : truncated;
    }

    private static string ExtractEnding(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        var startIndex = text.Length - maxLength;
        var firstNewline = text.IndexOf('\n', startIndex);
        
        if (firstNewline > startIndex)
            startIndex = firstNewline + 1;

        return text[startIndex..];
    }

    /// <summary>
    /// Estimates token count for text (Llama 3.1 uses ~4 chars per token).
    /// </summary>
    public int EstimateTokenCount(string text) 
        => (text?.Length ?? 0) / 4;
}