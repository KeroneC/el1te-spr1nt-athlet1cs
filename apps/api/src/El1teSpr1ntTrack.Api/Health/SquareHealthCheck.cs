using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace El1teSpr1ntTrack.Api.Health;

public sealed class SquareHealthCheck(
    ISquareClient squareClient,
    StoreSettings storeSettings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!storeSettings.Enabled)
        {
            return HealthCheckResult.Healthy("Store integration disabled.");
        }

        try
        {
            return await squareClient.CheckConnectionAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Square connection unavailable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Square connection unavailable.");
        }
    }
}
