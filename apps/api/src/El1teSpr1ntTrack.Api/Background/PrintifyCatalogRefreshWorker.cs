using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Commerce;

namespace El1teSpr1ntTrack.Api.Background;

public sealed class PrintifyCatalogRefreshWorker(
    IServiceScopeFactory scopeFactory,
    PrintifySettings settings,
    ILogger<PrintifyCatalogRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.HasCatalogCredentials)
        {
            logger.LogInformation("Printify catalog refresh is disabled or not configured.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Clamp(settings.RefreshMinutes, 15, 1440)));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPrintifyAdminService>();
                var result = await service.RefreshMappingsAsync(stoppingToken);
                logger.LogInformation(
                    "Printify catalog refresh checked {ProductCount} products and found {MappingIssueCount} mapping issues and {CostChangeCount} cost changes.",
                    result.ProductsChecked,
                    result.MappingIssues,
                    result.CostChanges);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Printify catalog refresh failed with no customer data logged.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }
}
