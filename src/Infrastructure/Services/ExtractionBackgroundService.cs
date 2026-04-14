using IntelliImport.Application.Services;
using IntelliImport.Domain.Enums;
using IntelliImport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntelliImport.Infrastructure.Services;

public sealed class ExtractionBackgroundService(
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
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var processor = scope.ServiceProvider.GetRequiredService<ExtractionJobProcessor>();

                // Find next queued or processing job
                var job = await dbContext.ExtractionJobs
                    .FirstOrDefaultAsync(j =>
                        j.Status == ExtractionJobStatus.Queued ||
                        j.Status == ExtractionJobStatus.Processing,
                        stoppingToken);

                if (job != null)
                {
                    logger.LogInformation("Processing job {Id}", job.Id);
                    await processor.ProcessJobAsync(job, stoppingToken);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    // No jobs, wait before checking again
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Background service cancellation requested");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background service");
                await Task.Delay(5000, stoppingToken); // Delay on error
            }
        }

        logger.LogInformation("Extraction background service stopped");
    }
}