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
        // Database
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3).CommandTimeout(180)
            ));

        // Repositories
        services.AddScoped<IExtractionRepository, ExtractionRepository>();

        // PDF Processing
        services.AddScoped<IPdfProcessor, PdfPigProcessor>();

        // Text Processing
        services.AddScoped<PdfTextProcessor>();

        // Llama Configuration
        services.Configure<LlamaOptions>(config.GetSection("Llama"));

        // Llama HttpClient with Polly
        services.AddHttpClient<IExtractionProvider, LlamaExtractionProvider>(client =>
        {
            var baseUrl = config["Llama:BaseUrl"] ?? "http://localhost:11434";
            var timeoutSecs = config.GetValue<int>("Llama:TimeoutSecs", 120);

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSecs);
        })
        .AddStandardResilienceHandler(config.GetSection("LlamaResilience"));

        // Job Queue (in-memory channel)
        services.AddSingleton<IJobQueue, JobQueue>();

        // Application Services
        services.AddScoped<ExtractionJobProcessor>();

        // Background Service
        services.AddHostedService<ExtractionBackgroundService>();

        return services;
    }
}
