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
        if (!settings.Enabled)
        {
            logger.LogInformation("Commerce outbox worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ICommerceOutboxProcessor>();
                processed = await processor.ProcessNextAsync(stoppingToken);
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
