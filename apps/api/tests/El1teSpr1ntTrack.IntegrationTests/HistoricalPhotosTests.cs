extern alias legacy;

using System.Security.Claims;
using System.Text.Json;
using El1teSpr1ntTrack.Api.Controllers.Admin;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using El1teSpr1ntTrack.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using legacy::El1teSpr1ntTrack.LegacyPhotoImport;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class HistoricalPhotosTests
{
    private const string Site = "https://www.el1tespr1ntathlet1cs.org";

    [Fact]
    public async Task Import_ResumesAfterBlobFailure_DeduplicatesAndPreservesReview()
    {
        using var archive = new TestArchive();
        var manifest = await archive.LoadAsync();
        var blobs = new MemoryBlobs { FailFirstPreview = true };
        await using var db = Database();
        var import = new PhotoImport(db, blobs);
        var first = new ImportResult();
        await PhotoImport.RunAsync(manifest, manifest.SelectPilot(), first, import, default);
        Assert.Equal(1, first.Failed);
        Assert.Single(blobs.Files);
        Assert.Empty(await db.LegacyPhotos.ToListAsync());

        var second = new ImportResult();
        await PhotoImport.RunAsync(manifest, manifest.SelectPilot(), second, import, default);
        Assert.Equal(1, second.Created);
        Assert.Equal(2, second.Sources);
        Assert.Equal(3, second.PageLinks);
        Assert.Equal(2, blobs.Files.Count);
        Assert.Equal(3, await db.LegacyPhotoPages.CountAsync());
        Assert.Equal(2, await db.LegacyPhotoSources.CountAsync());
        var photo = await db.LegacyPhotos.SingleAsync();
        Assert.Equal(LegacyPhotoReview.NeedsReview, photo.Review);
        Assert.Null(photo.CapturedOn);
        photo.Review = LegacyPhotoReview.Restricted;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var third = new ImportResult();
        await PhotoImport.RunAsync(manifest, manifest.SelectPilot(), third, import, default);
        Assert.Equal(1, third.Resumed);
        Assert.Equal(0, third.Created);
        Assert.Equal(LegacyPhotoReview.Restricted, (await db.LegacyPhotos.SingleAsync()).Review);
        Assert.Null(await new MediaRepository(db).GetActiveAsync(photo.Id, default));
    }

    [Fact]
    public async Task DryRun_RejectsBadManifestMissingAndTamperedFilesWithoutWriting()
    {
        using var archive = new TestArchive();
        await Assert.ThrowsAsync<InvalidDataException>(() => PhotoManifest.LoadAsync(archive.ManifestPath, new string('0', 64), default));
        var manifest = await archive.LoadAsync();
        var result = new ImportResult();
        await PhotoImport.RunAsync(manifest, manifest.Groups, result, null, default);
        Assert.Equal(1, result.Verified);
        Assert.Equal(0, result.Created);
        var original = Path.Combine(archive.Root, "originals", "other.jpg");
        var tampered = await File.ReadAllBytesAsync(original);
        tampered[10] ^= 1;
        await File.WriteAllBytesAsync(original, tampered);
        var bad = new ImportResult();
        await PhotoImport.RunAsync(manifest, manifest.Groups, bad, null, default);
        Assert.Equal(1, bad.Failed);
        File.Delete(original);
        var missing = new ImportResult();
        await PhotoImport.RunAsync(manifest, manifest.Groups, missing, null, default);
        Assert.Equal(1, missing.Failed);
    }

    [Fact]
    public async Task ReviewRequiresConsentAndOnlyApprovedFilesCanBeDownloaded()
    {
        await using var db = Database();
        var jpeg = Jpeg();
        var hash = PhotoManifest.Hash(jpeg);
        var photo = new LegacyPhoto { Sha256 = hash, Width = 2000, Height = 1000 };
        db.LegacyPhotos.Add(photo);
        await db.SaveChangesAsync();
        var blobs = new MemoryBlobs();
        await blobs.PutIfAbsentAsync(hash, false, jpeg, default);
        await blobs.PutIfAbsentAsync(hash, true, LegacyPhotoImages.InspectAndPreview(jpeg, "jpeg", 2000, 1000).Preview, default);
        using var services = new ServiceCollection().AddSingleton<ILegacyPhotoBlobs>(blobs).BuildServiceProvider();
        var userId = Guid.NewGuid();
        var controller = new HistoricalPhotosController(db, new() { Enabled = true }, new FixedClock(), services)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"))
            } }
        };

        Assert.IsType<NotFoundResult>(await new HistoricalPhotosController(db, new(), new FixedClock(), services).Preview(photo.Id, default));
        Assert.IsType<FileStreamResult>(await controller.Preview(photo.Id, default)).FileStream.Dispose();
        Assert.IsType<NotFoundResult>(await controller.Download(photo.Id, default));
        Assert.IsType<BadRequestObjectResult>(await controller.Review(photo.Id, new("ApprovedForSocial", false), default));
        Assert.IsType<NoContentResult>(await controller.Review(photo.Id, new("ApprovedForSocial", true), default));
        Assert.Equal(userId, photo.ReviewedByUserId);
        Assert.NotNull(photo.ReviewedAtUtc);
        var social = Assert.IsType<FileContentResult>(await controller.Download(photo.Id, default));
        Assert.Equal("image/jpeg", social.ContentType);
        Assert.DoesNotContain("Exif", System.Text.Encoding.ASCII.GetString(social.FileContents));
        using var data = SKData.CreateCopy(social.FileContents);
        using var codec = SKCodec.Create(data);
        Assert.Equal(800, codec.Info.Width);
        Assert.Equal(1600, codec.Info.Height);
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
        Assert.IsType<NoContentResult>(await controller.Review(photo.Id, new("Restricted", false), default));
        Assert.IsType<NotFoundResult>(await controller.Download(photo.Id, default));
    }

    [Fact]
    public void ApplyCannotTargetProductionOrBypassFullImportSignOff()
    {
        string[] baseline = ["--manifest", "manifest.json", "--manifest-sha256", new string('0', 64)];
        Assert.False(legacy::ImportArguments.Parse(baseline, "demo").Apply);
        Assert.Throws<InvalidDataException>(() => legacy::ImportArguments.Parse([.. baseline, "--apply", "--confirm-demo", "production"], "demo"));
        Assert.Throws<InvalidDataException>(() => legacy::ImportArguments.Parse([.. baseline, "--apply", "--confirm-demo", "demo", "--all"], "demo"));
    }

    private static El1teDbContext Database() => new(new DbContextOptionsBuilder<El1teDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static byte[] Jpeg()
    {
        using var bitmap = new SKBitmap(2000, 1000);
        bitmap.Erase(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        var bytes = data.ToArray();
        // EXIF APP1 with orientation 6 (90° clockwise); the output must rotate pixels and remove this segment.
        byte[] exif = [255, 225, 0, 34, 69, 120, 105, 102, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0,
            1, 0, 18, 1, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0];
        return [.. bytes[..2], .. exif, .. bytes[2..]];
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class MemoryBlobs : ILegacyPhotoBlobs
    {
        public bool FailFirstPreview { get; set; }
        public Dictionary<(string, bool), byte[]> Files { get; } = [];
        public Task<Stream?> ReadAsync(string hash, bool preview, CancellationToken token) =>
            Task.FromResult<Stream?>(Files.TryGetValue((hash, preview), out var bytes) ? new MemoryStream(bytes) : null);

        public Task PutIfAbsentAsync(string hash, bool preview, byte[] bytes, CancellationToken token)
        {
            if (preview && FailFirstPreview)
            {
                FailFirstPreview = false;
                throw new IOException();
            }
            Files.TryAdd((hash, preview), bytes);
            return Task.CompletedTask;
        }
    }

    private sealed class TestArchive : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("el1te-legacy-photo-test-").FullName;
        public string ManifestPath => Path.Combine(Root, "manifest.json");
        private readonly string hash;

        public TestArchive()
        {
            Directory.CreateDirectory(Path.Combine(Root, "originals"));
            var bytes = Jpeg();
            var photoHash = PhotoManifest.Hash(bytes);
            PhotoSource Source(string name, params string[] paths)
            {
                File.WriteAllBytes(Path.Combine(Root, "originals", name), bytes);
                return new PhotoSource($"https://images.example/{name}", paths.Select(path => new PhotoPage(Site + path)).ToList(),
                    "archived", name, $"originals/{name}", bytes.Length, photoHash, "jpeg", 2000, 1000);
            }
            var json = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1, sourceSite = Site, items = new[] { Source("first.jpg", "/gallery/training", "/team"), Source("other.jpg", "/coaches") }
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            File.WriteAllBytes(ManifestPath, json);
            hash = PhotoManifest.Hash(json);
        }

        public Task<PhotoManifest> LoadAsync() => PhotoManifest.LoadAsync(ManifestPath, hash, default);
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
