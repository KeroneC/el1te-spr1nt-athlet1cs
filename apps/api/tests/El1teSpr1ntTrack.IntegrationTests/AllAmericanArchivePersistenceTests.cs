using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class AllAmericanArchivePersistenceTests
{
    [Fact]
    public async Task SummaryOnlyYear_PublishesWithoutExposingIncompleteRoster()
    {
        await using var db = CreateContext();
        var media = AddMedia(db);
        await db.SaveChangesAsync();
        var service = Service(db);
        var created = await service.CreateAsync(Year(false, false, media.Id), CancellationToken.None);
        await service.AddMediaAsync(created.Id, new() { MediaAssetId = media.Id }, CancellationToken.None);
        await service.AddRecipientAsync(created.Id, new() { FirstName = "Draft", LastName = "Athlete" }, CancellationToken.None);
        await service.UpdateAsync(created.Id, Year(true, false, media.Id), CancellationToken.None);

        var publicYear = await service.GetPublicAsync("2026", CancellationToken.None);

        Assert.NotNull(publicYear);
        Assert.False(publicYear.DetailsComplete);
        Assert.Empty(publicYear.Recipients);
        Assert.Single(publicYear.Images);
        Assert.Equal(9, publicYear.AthleteCount);
        Assert.Equal(11, publicYear.MedalCount);
    }

    [Fact]
    public async Task DetailsComplete_RequiresRosterAndMedalRecipientTotalsToReconcile()
    {
        await using var db = CreateContext();
        var media = AddMedia(db);
        await db.SaveChangesAsync();
        var service = Service(db);
        var created = await service.CreateAsync(new() { Year = 2027, Title = "2027 Games", Summary = "Verified annual summary.", AthleteCount = 1, MedalCount = 1, HeroMediaAssetId = media.Id }, CancellationToken.None);
        await service.AddMediaAsync(created.Id, new() { MediaAssetId = media.Id }, CancellationToken.None);

        await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.UpdateAsync(created.Id,
            new() { Year = 2027, Title = "2027 Games", Summary = "Verified annual summary.", AthleteCount = 1, MedalCount = 1, HeroMediaAssetId = media.Id, IsPublished = true, DetailsComplete = true }, CancellationToken.None));

        db.ChangeTracker.Clear();
        service = Service(db);

        var withAthlete = await service.AddRecipientAsync(created.Id, new() { FirstName = "Alex", LastName = "Runner" }, CancellationToken.None);
        var recipientId = Assert.Single(withAthlete.Recipients).Id;
        await service.AddPerformanceAsync(created.Id, new() { EventName = "400 metres", RecipientIds = [recipientId] }, CancellationToken.None);
        var completed = await service.UpdateAsync(created.Id,
            new() { Year = 2027, Title = "2027 Games", Summary = "Verified annual summary.", AthleteCount = 1, MedalCount = 1, HeroMediaAssetId = media.Id, IsPublished = true, DetailsComplete = true }, CancellationToken.None);

        Assert.True(completed.DetailsComplete);
        var publicYear = await service.GetPublicAsync("2027", CancellationToken.None);
        Assert.Single(publicYear!.Recipients);
        Assert.Single(publicYear.Recipients[0].Results);

        var corrected = await service.UpdateAsync(created.Id,
            new() { Year = 2028, Title = "Corrected annual title", Summary = "Verified annual summary.", AthleteCount = 1, MedalCount = 1, HeroMediaAssetId = media.Id, IsPublished = true, DetailsComplete = true }, CancellationToken.None);
        Assert.Equal("2027", corrected.Slug);
        Assert.NotNull(await service.GetPublicAsync("2027", CancellationToken.None));

        await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            service.DeactivateRecipientAsync(created.Id, recipientId, CancellationToken.None));
    }

    [Fact]
    public async Task MediaReference_ProtectsAnnualAndRecipientAssets()
    {
        await using var db = CreateContext();
        var media = AddMedia(db);
        var year = new AllAmericanYear { Year = 2026, Slug = "2026", Title = "2026 Games", Summary = "Summary", HeroMediaAssetId = media.Id };
        year.Media.Add(new() { MediaAssetId = media.Id, MediaAsset = media });
        db.AllAmericanYears.Add(year);
        await db.SaveChangesAsync();

        Assert.True(await new MediaRepository(db).IsReferencedAsync(media.Id, media.PublicUrl, CancellationToken.None));
    }

    private static AllAmericanArchiveService Service(El1teDbContext db) => new(new AllAmericanArchiveRepository(db), new SystemClock());
    private static AllAmericanYearWriteDto Year(bool published, bool complete, Guid heroId) => new()
    {
        Year = 2026, Title = "2026 AAU Junior Olympic Games",
        Summary = "Nine El1te athletes earned All-American honors across individual and relay events at the 2026 AAU Junior Olympic Games.",
        AthleteCount = 9, MedalCount = 11, HeroMediaAssetId = heroId, IsPublished = published, DetailsComplete = complete
    };
    private static MediaAsset AddMedia(El1teDbContext db)
    {
        var user = new User { Email = "admin@example.invalid", FirstName = "Test", LastName = "Admin", PasswordHash = "unused" };
        var media = new MediaAsset { OriginalFileName = "all-american.jpg", StorageKey = "archive/image.jpg", ContentType = "image/jpeg", FileExtension = ".jpg", FileSizeBytes = 100, Width = 960, Height = 1280, Title = "All-American photograph", AltText = "Athlete holding an All-American medal", PublicUrl = "https://example.invalid/media/archive", UploadedByUser = user, UploadedByUserId = user.Id };
        db.AddRange(user, media); return media;
    }
    private static El1teDbContext CreateContext() => new(new DbContextOptionsBuilder<El1teDbContext>().UseInMemoryDatabase($"all-americans-{Guid.NewGuid():N}").Options);
}
