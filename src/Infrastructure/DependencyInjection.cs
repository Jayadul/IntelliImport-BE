using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Services;
using IntelliImport.Domain.Interfaces;
using IntelliImport.Infrastructure.AI;
using IntelliImport.Infrastructure.PDF;
using IntelliImport.Infrastructure.Persistence;
using IntelliImport.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntelliImport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // EF Core with extended command timeout
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3).CommandTimeout(180)
            ));

        services.AddScoped<IExtractionRepository, ExtractionRepository>();
        services.AddScoped<IPdfProcessor, PdfPigProcessor>();

        // Ollama options from configuration
        services.Configure<OllamaOptions>(config.GetSection("Ollama"));

        // HttpClient with resilience policies
        // CRITICAL: HttpClient timeout MUST be >= Polly TotalRequestTimeout
        services.AddHttpClient<IExtractionProvider, OllamaExtractionProvider>(client =>
        {
            var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
            var timeoutSecs = config.GetValue<int>("Ollama:TimeoutSecs", 180);
            
            client.BaseAddress = new Uri(baseUrl);
            // Set HttpClient timeout to be larger than Polly's TotalRequestTimeout
            client.Timeout = TimeSpan.FromSeconds(timeoutSecs);
        })
        .AddStandardResilienceHandler(config.GetSection("OllamaResilience"));

        services.AddScoped<ExtractionJobProcessor>();
        services.AddHostedService<ExtractionBackgroundService>();

        return services;
    }
}
