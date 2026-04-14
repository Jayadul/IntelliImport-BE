using Microsoft.Extensions.Http.Resilience;

namespace IntelliImport.Infrastructure.Resilience;

/// <summary>
/// Polly resilience pipeline configuration for Ollama HTTP client.
/// All settings are loaded from appsettings.json OllamaResilience section.
/// </summary>
public static class ResiliencePolicies
{
    // Configuration is handled by AddStandardResilienceHandler(IConfigurationSection)
    // which automatically binds settings to HttpStandardResilienceOptions.
    // This class is kept as a reference point for documentation purposes.
}
