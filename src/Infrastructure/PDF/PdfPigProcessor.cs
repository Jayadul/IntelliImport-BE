using System.Text;
using IntelliImport.Application.Abstractions;
using IntelliImport.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace IntelliImport.Infrastructure.PDF;

public sealed class PdfPigProcessor(ILogger<PdfPigProcessor> logger) : IPdfProcessor
{
    public Result<string> ExtractText(byte[] pdfBytes)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
            return Result<string>.Failure("PDF bytes are empty.", "EMPTY_PDF");

        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var sb             = new StringBuilder();
            var pageCount      = 0;

            foreach (Page page in document.GetPages())
            {
                pageCount++;
                // PdfPig preserves reading order via letter positions
                var words = page.GetWords();
                foreach (var word in words)
                    sb.Append(word.Text).Append(' ');

                sb.AppendLine(); // blank line between pages
            }

            var text = sb.ToString().Trim();

            if (string.IsNullOrWhiteSpace(text))
                return Result<string>.Failure(
                    "PDF contained no extractable text (may be image-only).", "NO_TEXT");

            logger.LogInformation("PdfPig extracted {Chars} chars from {Pages} pages",
                text.Length, pageCount);

            return Result<string>.Success(text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PdfPig failed to parse document");
            return Result<string>.Failure($"PDF parse error: {ex.Message}", "PDF_PARSE_ERROR");
        }
    }
}
