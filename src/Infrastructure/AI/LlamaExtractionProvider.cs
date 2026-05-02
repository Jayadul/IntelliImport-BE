using System.Net.Http.Json;
using System.Text.Json;
using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Models;
using IntelliImport.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntelliImport.Infrastructure.AI;

public sealed class LlamaExtractionProvider(
    HttpClient httpClient,
    IOptions<LlamaOptions> options,
    ILogger<LlamaExtractionProvider> logger) : IExtractionProvider
{
    private readonly LlamaOptions _opts = options.Value;

    public string ProviderName => $"ollama/{_opts.Model}";

    private static readonly string SystemPrompt = "You are an invoice data extraction assistant. Always respond with valid JSON only. No explanation, no markdown, no code fences.";

    public async Task<Result<ExtractionResult>> ExtractAsync(
        string pdfText, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(pdfText);

        var requestBody = new
        {
            model = _opts.Model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = prompt }
            },
            stream = false,
            temperature = _opts.Temperature
        };

        try
        {
            logger.LogInformation("Sending extraction request to Ollama model {Model}", _opts.Model);

            var response = await httpClient.PostAsJsonAsync("/api/chat", requestBody, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogDebug("Raw Ollama response: {Response}", responseBody);

            return ParseResponse(responseBody);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error calling Ollama");
            return Result<ExtractionResult>.Failure($"Ollama HTTP error: {ex.Message}", "OLLAMA_HTTP_ERROR");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Ollama request timed out");
            return Result<ExtractionResult>.Failure("Ollama request timed out", "OLLAMA_TIMEOUT");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error calling Ollama");
            return Result<ExtractionResult>.Failure($"Unexpected error: {ex.Message}", "OLLAMA_UNKNOWN");
        }
    }

    private Result<ExtractionResult> ParseResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            logger.LogDebug("Extracted message content: {Content}", messageContent);

            var cleaned = StripMarkdownFences(messageContent.Trim());
            logger.LogInformation("Cleaned AI response for parsing: {Cleaned}", cleaned);

            var result = JsonSerializer.Deserialize<ExtractionResult>(cleaned,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null)
                return Result<ExtractionResult>.Failure("Deserialized result was null", "PARSE_NULL");

            result.ConfidenceScore = result.ConfidenceScore == 0 ? 0.85m : result.ConfidenceScore;
            return Result<ExtractionResult>.Success(result);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Ollama JSON response: {Body}", responseBody);
            return Result<ExtractionResult>.Failure($"JSON parse error: {ex.Message}", "PARSE_ERROR");
        }
    }

    private static string StripMarkdownFences(string text)
    {
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text["```json".Length..].TrimStart();
        else if (text.StartsWith("```"))
            text = text["```".Length..].TrimStart();

        if (text.EndsWith("```"))
            text = text[..^"```".Length].TrimEnd();

        return text.Trim();
    }

    private static string BuildPrompt(string pdfText)
    {
        const string jsonSchema = """
            {
              "invoiceNo": "string or null",
              "vendor": "string or null",
              "date": "YYYY-MM-DD string or null",
              "total": "number or null",
              "confidenceScore": "number between 0.0 and 1.0",
              "lineItems": [
                {
                  "description": "string",
                  "quantity": "integer",
                  "unitPrice": "number",
                  "lineTotal": "number"
                }
              ]
            }
            """;

        return $"""
            Extract invoice data from the following document text and return ONLY a JSON object.
            
            REQUIRED JSON FORMAT (return nothing else, no explanation):
            {jsonSchema}
            
            DOCUMENT TEXT:
            {pdfText}
            """;
    }
}