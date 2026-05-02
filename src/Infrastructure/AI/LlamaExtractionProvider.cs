using System.Text;
using System.Text.Json;
using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Models;
using IntelliImport.Application.Services;
using IntelliImport.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntelliImport.Infrastructure.AI;

public sealed class LlamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1:8b";
    public double Temperature { get; set; } = 0.3;
    public int TimeoutSecs { get; set; } = 120;
}

/// <summary>
/// Llama 3.1 (8B) extraction provider with smart context windowing.
/// Optimized for enterprise invoice extraction with strict JSON schema enforcement.
/// Includes validation notes and business rule checks.
/// </summary>
public sealed class LlamaExtractionProvider(
    HttpClient httpClient,
    IOptions<LlamaOptions> options,
    PdfTextProcessor textProcessor,
    ILogger<LlamaExtractionProvider> logger
) : IExtractionProvider
{
    private readonly LlamaOptions _opts = options.Value;

    public string ProviderName => $"llama/{_opts.Model}";

    private const string SystemPrompt = """
        You are a precision invoice data extraction system.
        
        CRITICAL RULES:
        1. Extract ONLY to the provided JSON schema - no markdown, no explanation
        2. Use null for any missing or uncertain fields
        3. Calculate LineTotal = Quantity × UnitPrice for validation
        4. Provide a ValidationNote explaining any data quality issues
        5. Return VALID, well-formed JSON ONLY
        
        JSON Schema:
        {
          "InvoiceNo": "string or null",
          "Vendor": "string or null", 
          "Date": "YYYY-MM-DD or null",
          "Total": "decimal or null",
          "ConfidenceScore": "0.0-1.0",
          "ValidationNote": "string explaining any issues or uncertainties",
          "LineItems": [
            {
              "Description": "string",
              "Quantity": "integer",
              "UnitPrice": "decimal",
              "LineTotal": "decimal (must equal Quantity × UnitPrice)"
            }
          ]
        }
        
        VALIDATION RULES:
        - If you find discrepancies, document in ValidationNote
        - Set ConfidenceScore reflecting your certainty (0.0 = no confidence, 1.0 = absolute)
        - If data is ambiguous, use null and explain in ValidationNote
        """;

    public async Task<Result<ExtractionResult>> ExtractAsync(
        string pdfText, CancellationToken ct = default)
    {
        try
        {
            // Step 1: Smart context extraction (8K char limit for Llama 3.1)
            var smartText = textProcessor.ExtractSmartContext(pdfText);
            var tokenCount = textProcessor.EstimateTokenCount(smartText);
            
            logger.LogInformation(
                "Extracted smart context: {Chars} chars, ~{Tokens} tokens from {Total} chars",
                smartText.Length, tokenCount, pdfText.Length);

            // Step 2: Build request with smart context
            var userMessage = $"Extract invoice data:\n\n{smartText}";
            var requestBody = new
            {
                model = _opts.Model,
                stream = false,
                options = new 
                { 
                    temperature = _opts.Temperature,
                    num_ctx = 8192 // Llama 3.1 context window
                },
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userMessage }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            logger.LogDebug("Requesting Llama extraction: model={Model}, tokens={Tokens}",
                _opts.Model, tokenCount);

            // Step 3: Send request with Polly resilience
            var response = await httpClient.PostAsync("/api/chat", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Llama request failed: {StatusCode}\nResponse: {Response}",
                    response.StatusCode, responseBody[..Math.Min(500, responseBody.Length)]);
                
                return Result<ExtractionResult>.Failure($"Llama request failed with status {response.StatusCode}", "LLAMA_HTTP_ERROR");
            }

            // Step 4: Parse response
            using var doc = JsonDocument.Parse(responseBody);
            var rawMessage = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            logger.LogDebug("Llama response length: {Len} chars", rawMessage.Length);

            // Step 5: Clean JSON (remove markdown if present)
            var cleanJson = StripMarkdownFences(rawMessage);

            // Step 6: Deserialize with validation
            var result = JsonSerializer.Deserialize<ExtractionResult>(cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null)
            {
                return Result<ExtractionResult>.Failure("Llama returned null result", "NULL_RESULT");
            }

            logger.LogInformation(
                "Extraction successful: Invoice={Invoice}, Vendor={Vendor}, Confidence={Confidence:F2}",
                result.InvoiceNo ?? "unknown",
                result.Vendor ?? "unknown",
                result.ConfidenceScore);

            return Result<ExtractionResult>.Success(result);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Llama JSON response");
            return Result<ExtractionResult>.Failure($"Invalid JSON from Llama: {ex.Message}", "INVALID_JSON");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Llama HTTP request failed");
            return Result<ExtractionResult>.Failure($"Llama connection failed: {ex.Message}", "LLAMA_UNREACHABLE");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Llama request cancelled (timeout)");
            return Result<ExtractionResult>.Failure("Llama request timeout", "LLAMA_TIMEOUT");
        }
    }

    private static string StripMarkdownFences(string raw)
    {
        var trimmed = raw.Trim();
        
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
                trimmed = trimmed[(firstNewline + 1)..];
            
            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3];
        }

        return trimmed.Trim();
    }
}