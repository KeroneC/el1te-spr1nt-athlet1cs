using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class GalleryPersistenceTests
{
    [Fact]
    public async Task AddMediaAsync_InsertsAssociationForExistingAlbum()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"gallery-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new El1teDbContext(options);
        var user = new User
        {
            Email = "admin@example.invalid",
            FirstName = "Test",
            LastName = "Admin",
            PasswordHash = "not-used"
        };
        var media = new MediaAsset
        {
            OriginalFileName = "meet.png",
            StorageKey = "2026/06/test.png",
            ContentType = "image/png",
            FileExtension = ".png",
            FileSizeBytes = 100,
            Width = 10,
            Height = 10,
            Title = "Meet photo",
            AltText = "Athletes at a meet",
            PublicUrl = "http://localhost:5126/media/test",
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };
        var album = new GalleryAlbum
        {
            Title = "Summer meet",
            Slug = "summer-meet",
            Description = "Meet photos"
        };
        dbContext.AddRange(user, media, album);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new GalleryService(
            new GalleryRepository(dbContext),
            new SlugGenerator(),
            new SystemClock());

        var result = await service.AddMediaAsync(
            album.Id,
            new GalleryAlbumMediaWriteDto
            {
                MediaAssetId = media.Id,
                CaptionOverride = "Finish line",
                DisplayOrder = 2
            },
            CancellationToken.None);

        var stored = await dbContext.GalleryAlbumMedia.SingleAsync();
        Assert.Equal(album.Id, stored.GalleryAlbumId);
        Assert.Equal(media.Id, stored.MediaAssetId);
        Assert.Equal("Finish line", stored.CaptionOverride);
        Assert.Equal(2, stored.DisplayOrder);
        Assert.Single(result.Media);
        Assert.Equal(media.Id, result.Media[0].MediaAssetId);
    }
}
