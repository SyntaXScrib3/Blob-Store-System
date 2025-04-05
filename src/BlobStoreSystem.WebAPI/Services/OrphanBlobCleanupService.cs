using BlobStoreSystem.Infrastructure.Data;
using BlobStoreSystem.Domain.Services;
using Microsoft.EntityFrameworkCore;
using System;

namespace BlobStoreSystem.WebAPI.Services;

public class OrphanBlobCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrphanBlobCleanupService> _logger;

    public OrphanBlobCleanupService(IServiceProvider serviceProvider,
                                    ILogger<OrphanBlobCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Runs when the service starts. We'll loop every X minutes/hours.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanOrphanBlobsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during orphan blob cleanup.");
            }

            // Wait 1 hour, for example
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanOrphanBlobsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlobStoreDbContext>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageProvider>();

        // 1. Find all blobs where ReferenceCount <= 0
        var orphans = await dbContext.Blobs
            .Where(b => b.ReferenceCount <= 0)
            .ToListAsync();

        foreach (var blob in orphans)
        {
            // 2. Delete from storage
            await blobStorage.DeleteBlobAsync(blob.Id);

            // 3. Remove from DB
            dbContext.Blobs.Remove(blob);
        }

        await dbContext.SaveChangesAsync();
    }
}
