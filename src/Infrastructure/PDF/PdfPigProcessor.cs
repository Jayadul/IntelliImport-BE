using IntelliImport.Domain.Interfaces;
using IntelliImport.Domain.Results;
using UglyToad.PdfPig;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Infrastructure.PDF;

public sealed class PdfPigProcessor(
    ILogger<PdfPigProcessor> logger
) : IPdfProcessor
{
    public Result<string> ExtractText(byte[] fileBytes)
    {
        try
        {
            using var document = PdfDocument.Open(fileBytes);

            var textBuilder = new System.Text.StringBuilder();
            var pageCount = document.NumberOfPages;

            for (int i = 1; i <= pageCount; i++)
            {
                var page = document.GetPage(i);
                var text = page.Text;
                textBuilder.AppendLine($"--- Page {i} ---");
                textBuilder.AppendLine(text);
                textBuilder.AppendLine();
            }

            var extractedText = textBuilder.ToString();
            logger.LogInformation(
                "PdfPig extracted {Chars} chars from {Pages} pages",
                extractedText.Length, pageCount);

            return Result<string>.Success(extractedText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PDF text extraction failed");
            return Result<string>.Failure(
                $"PDF extraction error: {ex.Message}",
                "PDF_PARSE_ERROR");
        }
    }
}
