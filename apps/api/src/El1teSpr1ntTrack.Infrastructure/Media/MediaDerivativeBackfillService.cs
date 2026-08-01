using System.Security.Cryptography;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed record MediaBackfillReport(int Processed, int Skipped, int Failed, long OriginalBytes,
    long DerivativeBytes, IReadOnlyDictionary<Guid, string> OriginalSha256, IReadOnlyList<string> Errors);

public sealed class MediaDerivativeBackfillService(
    El1teDbContext dbContext,
    IMediaStorage storage,
    IMediaDerivativeGenerator generator)
{
    public async Task<MediaBackfillReport> RunAsync(
        bool includeSkippedHashes = true,
        CancellationToken cancellationToken = default)
    {
        var processed = 0; var skipped = 0; var failed = 0;
        long originalBytes = 0; long derivativeBytes = 0;
        var hashes = new Dictionary<Guid, string>();
        var errors = new List<string>();
        var assetIds = await dbContext.MediaAssets.AsNoTracking().OrderBy(value => value.CreatedAtUtc)
            .Select(value => value.Id).ToListAsync(cancellationToken);

        foreach (var assetId in assetIds)
        {
            var newKeys = new List<string>();
            try
            {
                var asset = await dbContext.MediaAssets.Include(value => value.Derivatives)
                    .SingleAsync(value => value.Id == assetId, cancellationToken);
                var expectedWidths = new[] { 480, 960, 1600 }.Where(width => width <= asset.Width).ToArray();
                if (!includeSkippedHashes && expectedWidths.All(width =>
                        asset.Derivatives.Any(existing => existing.RequestedWidth == width)))
                {
                    skipped++;
                    continue;
                }
                await using var original = await storage.OpenReadAsync(asset.StorageKey, cancellationToken);
                if (original is null) throw new InvalidOperationException("Original file is missing.");
                await using var buffer = new MemoryStream();
                await original.CopyToAsync(buffer, cancellationToken);
                var sourceBytes = buffer.ToArray();
                hashes[asset.Id] = Convert.ToHexString(SHA256.HashData(sourceBytes));
                var generated = generator.Generate(new MemoryStream(sourceBytes, writable: false));
                var missing = generated.Where(item => asset.Derivatives.All(existing => existing.RequestedWidth != item.RequestedWidth)).ToList();
                if (missing.Count == 0) { skipped++; continue; }

                foreach (var item in missing)
                {
                    await using var derivativeSource = new MemoryStream(item.Content, writable: false);
                    var stored = await storage.SaveAsync(derivativeSource, ".webp", cancellationToken);
                    newKeys.Add(stored.StorageKey);
                    asset.Derivatives.Add(new MediaDerivative
                    {
                        RequestedWidth = item.RequestedWidth, Width = item.Width, Height = item.Height,
                        ContentType = "image/webp", StorageKey = stored.StorageKey,
                        FileSizeBytes = item.Content.LongLength, Sha256 = item.Sha256
                    });
                    derivativeBytes += item.Content.LongLength;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                originalBytes += sourceBytes.LongLength;
                processed++;
            }
            catch (Exception exception)
            {
                failed++;
                dbContext.ChangeTracker.Clear();
                foreach (var key in newKeys)
                {
                    try { await storage.DeleteAsync(key, cancellationToken); } catch { /* Report the primary failure only. */ }
                }
                errors.Add($"{assetId}: {exception.GetType().Name}");
            }
        }

        return new MediaBackfillReport(processed, skipped, failed, originalBytes, derivativeBytes, hashes, errors);
    }
}
