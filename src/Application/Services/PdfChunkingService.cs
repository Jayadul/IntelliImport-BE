using System.Text;

namespace IntelliImport.Application.Services;

public sealed class PdfChunkingService
{
    private const int ChunkTokenEstimate = 1000;  // ~4 chars per token
    private const int ChunkSize = 4000;           // chars (1000 tokens)
    private const int OverlapSize = 400;          // 10% overlap to maintain context

    /// <summary>
    /// Splits large PDF text into manageable chunks with overlap.
    /// Estimates ~4 chars per token (conservative estimate).
    /// </summary>
    public static List<string> ChunkPdfText(string pdfText)
    {
        if (string.IsNullOrEmpty(pdfText))
            return [];

        var chunks = new List<string>();
        var text = pdfText.Trim();

        if (text.Length <= ChunkSize)
        {
            chunks.Add(text);
            return chunks;
        }

        // Split by paragraphs first to maintain context
        var paragraphs = text.Split(
            new[] { "\n\n", "\r\n\r\n" },
            StringSplitOptions.None);

        var currentChunk = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > ChunkSize && currentChunk.Length > 0)
            {
                // Save current chunk with overlap marker
                chunks.Add(currentChunk.ToString().Trim());
                
                // Start new chunk with overlap from end of previous chunk
                var overlapStart = Math.Max(0, currentChunk.Length - OverlapSize);
                var overlap = currentChunk.ToString()[overlapStart..];
                currentChunk.Clear();
                currentChunk.Append(overlap);
            }

            if (currentChunk.Length > 0)
                currentChunk.Append("\n\n");
                
            currentChunk.Append(paragraph);
        }

        // Add final chunk
        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString().Trim());

        return chunks;
    }

    /// <summary>
    /// Estimates token count for text (rough estimate).
    /// </summary>
    public static int EstimateTokenCount(string text) 
        => (text?.Length ?? 0) / 4;
}