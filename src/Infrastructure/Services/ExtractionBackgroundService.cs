using IntelliImport.Application.Abstractions;
using IntelliImport.Application.Services;
using IntelliImport.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Infrastructure.Services;

public sealed class ExtractionBackgroundService(
    IJobQueue jobQueue,
    IServiceProvider serviceProvider,
    ILogger<ExtractionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Extraction background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await jobQueue.DequeueAsync(stoppingToken);

                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var processor = scope.ServiceProvider.GetRequiredService<ExtractionJobProcessor>();

                var job = await db.ExtractionJobs.FindAsync(new object[] { jobId }, stoppingToken);
                if (job is null)
                {
                    logger.LogWarning("Job {Id} not found, skipping", jobId);
                    continue;
                }

                await processor.ProcessJobAsync(job, stoppingToken);

                // Entity is already tracked by this DbContext — just save
                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Job {Id} persisted with status {Status}",
                    job.Id, job.Status);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Background service stopping");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in background service");
                await Task.Delay(5_000, stoppingToken);
            }
        }

        logger.LogInformation("Extraction background service stopped");
    }
}