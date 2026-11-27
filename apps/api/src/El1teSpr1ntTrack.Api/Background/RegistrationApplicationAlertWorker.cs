using El1teSpr1ntTrack.Api.Configuration;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Api.Background;

public sealed class RegistrationApplicationAlertWorker(
    IServiceScopeFactory scopes,
    RegistrationApplicationAlertSettings settings,
    ILogger<RegistrationApplicationAlertWorker> logger) : BackgroundService
{
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (settings.InvalidConfiguration)
            logger.LogError("Registration application alerts are disabled because their configuration is invalid.");
        if (!settings.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await ProcessNextAsync(stoppingToken))
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError("Registration alert polling failed with {FailureType}.", exception.GetType().Name);
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    public async Task<bool> ProcessNextAsync(CancellationToken token)
    {
        if (!settings.Enabled)
            return false;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<El1teDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var ready = db.RegistrationApplicationAlerts.Where(x => x.CompletedAtUtc == null && x.AvailableAtUtc <= now &&
            (x.LockedUntilUtc == null || x.LockedUntilUtc <= now));
        var next = await ready.OrderBy(x => x.AvailableAtUtc).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Recipient, x.AttemptCount }).FirstOrDefaultAsync(token);
        if (next is null)
            return false;

        // The conditional update lets only one process claim a message, even across App Service instances.
        var lockId = Guid.NewGuid();
        if (await ready.Where(x => x.Id == next.Id).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.LockId, lockId)
            .SetProperty(x => x.LockedUntilUtc, now.AddMinutes(3))
            .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), token) != 1)
            return true;

        var owned = db.RegistrationApplicationAlerts.Where(x => x.Id == next.Id && x.LockId == lockId);
        string? failure = null;
        var complete = true;
        var recipientAllowed = settings.Allows(next.Recipient);
        if (!recipientAllowed || next.AttemptCount >= MaxAttempts)
        {
            failure = recipientAllowed ? "AttemptLimit" : "RecipientRemoved";
        }
        else
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(90));
                var sender = scope.ServiceProvider.GetRequiredService<ITransactionalEmailSender>();
                await sender.SendAsync(settings.CreateEmail(next.Recipient), timeout.Token);
            }
            catch (Exception exception) when (!token.IsCancellationRequested)
            {
                failure = exception.GetType().Name;
                complete = next.AttemptCount + 1 >= MaxAttempts;
                if (complete)
                    logger.LogError("Registration alert delivery exhausted retries: {FailureType}.", failure);
                else
                    logger.LogWarning("Registration alert delivery will retry after {FailureType}.", failure);
            }
        }
        now = clock.UtcNow;
        await owned.ExecuteUpdateAsync(set => set
            .SetProperty(x => x.LockId, (Guid?)null)
            .SetProperty(x => x.LockedUntilUtc, (DateTimeOffset?)null)
            .SetProperty(x => x.SafeLastError, failure)
            .SetProperty(x => x.CompletedAtUtc, complete ? now : (DateTimeOffset?)null)
            .SetProperty(x => x.AvailableAtUtc, now.AddSeconds(20 * (next.AttemptCount + 1))), token);
        return true;
    }
}
