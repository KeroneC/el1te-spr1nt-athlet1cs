using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Commerce;

namespace El1teSpr1ntTrack.Api.Background;

public sealed class CommerceOutboxWorker(
    IServiceScopeFactory scopeFactory,
    StoreSettings settings,
    ILogger<CommerceOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.CommerceOperationsEnabled)
        {
            logger.LogInformation("Commerce outbox worker is disabled.");
            return;
        }

        var nextMaintenanceAtUtc = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ICommerceOutboxProcessor>();
                processed = await processor.ProcessNextAsync(stoppingToken);
                if (!processed && DateTimeOffset.UtcNow >= nextMaintenanceAtUtc)
                {
                    var orders = scope.ServiceProvider.GetRequiredService<IStoreOrderService>();
                    processed = await orders.RunMaintenanceAsync(stoppingToken) > 0;
                    nextMaintenanceAtUtc = DateTimeOffset.UtcNow.AddMinutes(
                        Math.Clamp(settings.ReconciliationMinutes, 1, 60));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Commerce outbox polling failed with {FailureType}.",
                    exception.GetType().Name);
            }

            if (!processed)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Clamp(settings.OutboxPollSeconds, 1, 60)),
                    stoppingToken);
            }
        }
    }
}
