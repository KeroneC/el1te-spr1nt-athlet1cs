using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Infrastructure.Media;

namespace El1teSpr1ntTrack.Api.Background;

public sealed class MediaDerivativeBackfillWorker(
    IServiceScopeFactory scopeFactory,
    MediaStorageOptions options,
    ILogger<MediaDerivativeBackfillWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.BackfillDerivativesOnStartup)
        {
            logger.LogInformation("Media derivative startup backfill is disabled.");
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<MediaDerivativeBackfillService>();
            var report = await service.RunAsync(includeSkippedHashes: false, stoppingToken);
            logger.LogInformation(
                "Media derivative startup backfill completed: {Processed} processed, {Skipped} skipped, {Failed} failed, {OriginalBytes} source bytes, {DerivativeBytes} derivative bytes.",
                report.Processed,
                report.Skipped,
                report.Failed,
                report.OriginalBytes,
                report.DerivativeBytes);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Media derivative startup backfill failed with {FailureType}; original media remains available.",
                exception.GetType().Name);
        }
    }
}
