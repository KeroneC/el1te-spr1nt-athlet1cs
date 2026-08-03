using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class MediaDerivativeBackfillTests
{
    [Fact]
    public async Task RunAsync_InsertsDerivativeForExistingMediaAsset()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"media-backfill-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new El1teDbContext(options);
        var user = new User
        {
            Email = "admin@example.invalid",
            FirstName = "Test",
            LastName = "Admin",
            PasswordHash = "not-used"
        };
        var asset = new MediaAsset
        {
            OriginalFileName = "portrait.jpg",
            StorageKey = "original.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            FileSizeBytes = 4,
            Width = 960,
            Height = 540,
            Title = "Portrait",
            AltText = "Athlete portrait",
            PublicUrl = "http://localhost:5126/media/test",
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };
        dbContext.AddRange(user, asset);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var storage = new TestMediaStorage(new Dictionary<string, byte[]>
        {
            [asset.StorageKey] = [1, 2, 3, 4]
        });
        var service = new MediaDerivativeBackfillService(
            dbContext,
            storage,
            new TestDerivativeGenerator());

        var report = await service.RunAsync(cancellationToken: CancellationToken.None);

        Assert.Equal(1, report.Processed);
        Assert.Equal(0, report.Failed);
        Assert.Equal(4, report.OriginalBytes);
        Assert.Equal(3, report.DerivativeBytes);
        var derivative = await dbContext.MediaDerivatives.SingleAsync();
        Assert.Equal(asset.Id, derivative.MediaAssetId);
        Assert.Equal(480, derivative.RequestedWidth);
        Assert.Equal("image/webp", derivative.ContentType);
        Assert.True(await storage.ExistsAsync(derivative.StorageKey, CancellationToken.None));
    }

    private sealed class TestDerivativeGenerator : IMediaDerivativeGenerator
    {
        public IReadOnlyList<GeneratedMediaDerivative> Generate(Stream source) =>
        [
            new GeneratedMediaDerivative(480, 480, 270, [5, 6, 7], new string('A', 64))
        ];
    }

    private sealed class TestMediaStorage(Dictionary<string, byte[]> files) : IMediaStorage
    {
        private int _nextKey;

        public async Task<StoredMediaFile> SaveAsync(
            Stream stream,
            string extension,
            CancellationToken cancellationToken)
        {
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            var key = $"generated-{++_nextKey}{extension}";
            files[key] = buffer.ToArray();
            return new StoredMediaFile(key);
        }

        public async Task SaveAsAsync(
            Stream stream,
            string storageKey,
            CancellationToken cancellationToken)
        {
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            files[storageKey] = buffer.ToArray();
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(files.TryGetValue(storageKey, out var bytes)
                ? new MemoryStream(bytes, writable: false)
                : null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            files.Remove(storageKey);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken) =>
            Task.FromResult(files.ContainsKey(storageKey));
    }
}
