using System.Text;
using System.Text.Json;
using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Models;
using IntelliImport.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntelliImport.Infrastructure.AI;

public sealed class OllamaOptions
{
    public string BaseUrl      { get; set; } = "http://localhost:11434";
    public string Model        { get; set; } = "phi3";
    public int    MaxRetries   { get; set; } = 3;
    public double Temperature  { get; set; } = 0.0;
    public int    TimeoutSecs  { get; set; } = 120;
}

/// <summary>
/// Ollama Phi-3 extraction provider. Uses strict JSON prompt engineering.
/// Polly resilience pipeline is applied at the HttpClient registration level.
/// </summary>
public sealed class OllamaExtractionProvider(
    HttpClient              httpClient,
    IOptions<OllamaOptions> options,
    ILogger<OllamaExtractionProvider> logger
) : IExtractionProvider
{
    private readonly OllamaOptions _opts = options.Value;

    public string ProviderName => $"ollama/{_opts.Model}";

    private const string SystemPrompt = """
        You are a precise invoice data extraction AI.
        Extract invoice data from the provided text and respond ONLY with valid JSON.
        Do NOT include any explanation, markdown, or text outside the JSON.
        
        The JSON must strictly follow this schema:
        {
          "InvoiceNo":       "string or null",
          "Vendor":          "string or null",
          "Date":            "YYYY-MM-DD string or null",
          "Total":           "number or null",
          "ConfidenceScore": "number between 0.0 and 1.0",
          "LineItems": [
            {
              "Description": "string",
              "Quantity":    "integer",
              "UnitPrice":   "number",
              "LineTotal":   "number"
            }
          ]
        }
        
        Rules:
        - ConfidenceScore reflects your certainty about the extracted data.
        - LineTotal MUST equal Quantity * UnitPrice for each line.
        - If a field is not found, use null.
        - Return ONLY the JSON object.
        """;

    public async Task<Result<ExtractionResult>> ExtractAsync(
        string pdfText, CancellationToken ct = default)
    {
        var userMessage = $"Extract invoice data from the following document text:\n\n{pdfText}";

        var requestBody = new
        {
            model  = _opts.Model,
            stream = false,
            options = new { temperature = _opts.Temperature },
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user",   content = userMessage  }
            }
        };

        try
        {
            var json    = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            logger.LogDebug("Sending extraction request to Ollama ({Model})", _opts.Model);
            logger.LogDebug("Request payload: {Payload}", json); // Log the request

            var response = await httpClient.PostAsync("/api/chat", content, ct);
            
            // Log response status and body BEFORE calling EnsureSuccessStatusCode
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogDebug("Ollama HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
            
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(responseBody);
            var rawMessage = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            logger.LogDebug("Ollama raw response length: {Len} chars", rawMessage.Length);

            // Strip markdown code fences if model wraps in ```json ... ```
            var cleanJson = StripMarkdownFences(rawMessage);

            var result = JsonSerializer.Deserialize<ExtractionResult>(cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null)
                return Result<ExtractionResult>.Failure("AI returned null result.", "NULL_RESULT");

            return Result<ExtractionResult>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ollama HTTP request failed");
            return Result<ExtractionResult>.Failure(
                $"Ollama connection failed: {ex.Message}", "OLLAMA_UNREACHABLE");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Ollama JSON response");
            return Result<ExtractionResult>.Failure(
                $"AI returned invalid JSON: {ex.Message}", "INVALID_JSON");
        }
    }

    private static string StripMarkdownFences(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0) trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```")) trimmed = trimmed[..^3];
        }
        return trimmed.Trim();
    }
}
