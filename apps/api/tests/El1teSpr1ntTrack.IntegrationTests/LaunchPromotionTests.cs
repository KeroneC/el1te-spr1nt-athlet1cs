extern alias promotion;

using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;
using PromotionArguments = promotion::PromotionArguments;
using PromotionEngine = promotion::PromotionEngine;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class LaunchPromotionTests
{
    [Fact]
    public async Task Manifest_ExcludesPrivateTables_HashesMedia_AndImportsIdempotently()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"promotion-source-{Guid.NewGuid():N}");
        var destinationRoot = Path.Combine(Path.GetTempPath(), $"promotion-destination-{Guid.NewGuid():N}");
        try
        {
            await using var source = Context("source");
            var uploader = new User { Email = "demo@example.test", FirstName = "Demo", LastName = "Admin", PasswordHash = "not-exported" };
            var media = new MediaAsset { UploadedByUserId = uploader.Id, OriginalFileName = "team.png", StorageKey = "2026/08/team.png", ContentType = "image/png", FileExtension = ".png", FileSizeBytes = 4, Width = 1, Height = 1, Title = "Team", AltText = "Team", PublicUrl = $"https://demo-api.example/media/{Guid.NewGuid()}" };
            media.PublicUrl = $"https://demo-api.example/media/{media.Id}";
            source.AddRange(uploader, media,
                new Announcement { Title = "Published", Slug = "published", Summary = "Summary", Body = "Body", IsPublished = true, ImageUrl = media.PublicUrl },
                new Announcement { Title = "Draft", Slug = "draft", Summary = "Summary", Body = "Body", IsPublished = false },
                new ContactSubmission { Name = "Private", Email = "private@example.test", Message = "Do not export" });
            await source.SaveChangesAsync();
            var sourceStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = sourceRoot });
            await sourceStorage.SaveAsAsync(new MemoryStream([1, 2, 3, 4]), media.StorageKey, default);
            var args = Arguments(sourceRoot, destinationRoot);

            var manifest = await PromotionEngine.ExportAsync(source, args, sourceStorage);

            Assert.DoesNotContain(manifest.Records, value => value.Type is nameof(User) or nameof(ContactSubmission));
            Assert.Contains(manifest.Records, value => value.Type == nameof(Announcement) && value.Status == "Draft" && !value.Include);
            var mediaRecord = Assert.Single(manifest.Records, value => value.Type == nameof(MediaAsset));
            Assert.True(mediaRecord.Include);
            Assert.Equal(64, mediaRecord.BlobSha256!.Length);
            Assert.Contains(media.Id.ToString(), Assert.Single(manifest.Records, value => value.Type == nameof(Announcement) && value.Include).Dependencies);

            await using var destination = Context("destination");
            var destinationStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = destinationRoot });
            var first = await PromotionEngine.ImportAsync(destination, manifest, args with { Apply = true }, sourceStorage, destinationStorage);
            var second = await PromotionEngine.ImportAsync(destination, manifest, args with { Apply = true }, sourceStorage, destinationStorage);
            Assert.True(first.Inserted > 0);
            Assert.Equal(0, second.Inserted);
            Assert.True(second.Updated > 0);
            Assert.Equal(args.BootstrapUserId, (await destination.MediaAssets.SingleAsync()).UploadedByUserId);
            Assert.True(await destinationStorage.ExistsAsync(media.StorageKey, default));
        }
        finally
        {
            if (Directory.Exists(sourceRoot)) Directory.Delete(sourceRoot, true);
            if (Directory.Exists(destinationRoot)) Directory.Delete(destinationRoot, true);
        }
    }

    [Fact]
    public async Task Validation_RejectsASelectedRecordWhoseDependencyIsExcluded()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"promotion-source-{Guid.NewGuid():N}");
        var destinationRoot = Path.Combine(Path.GetTempPath(), $"promotion-destination-{Guid.NewGuid():N}");
        try
        {
            await using var source = Context("dependency-source");
            var uploader = new User { Email = "demo@example.test", FirstName = "Demo", LastName = "Admin", PasswordHash = "not-exported" };
            var media = new MediaAsset { UploadedByUserId = uploader.Id, OriginalFileName = "draft.png", StorageKey = "2026/08/draft.png", ContentType = "image/png", FileExtension = ".png", FileSizeBytes = 4, Width = 1, Height = 1, Title = "Draft", AltText = "Draft" };
            media.PublicUrl = $"https://demo-api.example/media/{media.Id}";
            source.AddRange(uploader, media, new Announcement { Title = "Draft", Slug = "draft", Summary = "Summary", Body = "Body", IsPublished = false, ImageUrl = media.PublicUrl });
            await source.SaveChangesAsync();
            var sourceStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = sourceRoot });
            await sourceStorage.SaveAsAsync(new MemoryStream([1, 2, 3, 4]), media.StorageKey, default);
            var args = Arguments(sourceRoot, destinationRoot);
            var manifest = await PromotionEngine.ExportAsync(source, args, sourceStorage);
            var selected = manifest with { Records = manifest.Records.Select(value => value.Type == nameof(Announcement) ? value with { Include = true } : value).ToList() };

            var error = Assert.Throws<InvalidOperationException>(() => PromotionEngine.Validate(selected, args));

            Assert.Contains("excluded dependencies", error.Message);
            Assert.Equal(64, Assert.Single(manifest.Records, value => value.Type == nameof(MediaAsset)).BlobSha256!.Length);
        }
        finally
        {
            if (Directory.Exists(sourceRoot)) Directory.Delete(sourceRoot, true);
            if (Directory.Exists(destinationRoot)) Directory.Delete(destinationRoot, true);
        }
    }

    private static El1teDbContext Context(string suffix) => new(new DbContextOptionsBuilder<El1teDbContext>()
        .UseInMemoryDatabase($"promotion-{suffix}-{Guid.NewGuid():N}").Options);
    private static PromotionArguments Arguments(string sourceRoot, string destinationRoot) => new(
        "export", "demo", "production", "unused", "unused", "manifest.json",
        "https://demo-api.example", "https://api.example", false, "", Guid.NewGuid(),
        sourceRoot, destinationRoot, "", "", "media");
}
