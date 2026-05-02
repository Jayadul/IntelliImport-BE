using System.Threading.Channels;
using IntelliImport.Application.Services;
using IntelliImport.Domain.Enums;
using IntelliImport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Infrastructure.Services;

public class QueuedExtractionService(
    ChannelReader<Guid> extractionQueue,
    IServiceProvider serviceProvider,
    ILogger<QueuedExtractionService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Extraction queue service started");

        try
        {
            await foreach (var jobId in extractionQueue.ReadAllAsync(stoppingToken))
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Extraction queue service stopped");
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var processor = scope.ServiceProvider.GetRequiredService<ExtractionJobProcessor>();

            var job = await db.ExtractionJobs.FindAsync(new object[] { jobId }, cancellationToken: ct);
            if (job is null) return;

            logger.LogInformation("Processing job {JobId}: {File}", jobId, job.FileName);
            await processor.ProcessJobAsync(job, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed", jobId);
        }
    }
}

public static class QueuedExtractionExtensions
{
    public static IServiceCollection AddQueuedExtraction(this IServiceCollection services)
    {
        var channel = Channel.CreateUnbounded<Guid>();
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);
        services.AddHostedService<QueuedExtractionService>();
        return services;
    }
}