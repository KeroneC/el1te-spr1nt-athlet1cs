using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class HallOfFamePersistenceTests
{
    [Fact]
    public async Task PublicList_ExcludesInactiveAndUsesOrderNameAndPagination()
    {
        await using var db = CreateContext();
        db.HallOfFameInductees.AddRange(
            Inductee("Zulu", 1, true),
            Inductee("Alpha", 1, true),
            Inductee("Hidden", 0, false));
        await db.SaveChangesAsync();

        var result = await new PublicCmsRepository(db).GetHallOfFameInducteesAsync(
            new HallOfFameInducteeQueryOptions(1, 1), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task AdminList_FiltersByNameAffiliationStateAndYear()
    {
        await using var db = CreateContext();
        var matching = Inductee("Ada Runner", 1, false);
        matching.Affiliation = "State University";
        matching.InductionYear = 2026;
        db.HallOfFameInductees.AddRange(matching, Inductee("Other Athlete", 2, true));
        await db.SaveChangesAsync();

        var result = await new AdminCmsRepository(db).GetHallOfFameInducteesAsync(
            new AdminHallOfFameInducteeOptions("State", false, 2026), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(matching.Id, item.Id);
    }

    [Fact]
    public async Task MediaReference_ProtectsPhotoUsedByInactiveInductee()
    {
        await using var db = CreateContext();
        var item = Inductee("Draft", 1, false);
        item.PhotoUrl = "/media/hall-photo.jpg";
        db.HallOfFameInductees.Add(item);
        await db.SaveChangesAsync();

        var referenced = await new MediaRepository(db).IsReferencedAsync(
            Guid.NewGuid(), item.PhotoUrl, CancellationToken.None);

        Assert.True(referenced);
    }

    private static El1teDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"hall-of-fame-{Guid.NewGuid():N}")
            .Options;
        return new El1teDbContext(options);
    }

    private static HallOfFameInductee Inductee(string name, int order, bool active) => new()
    {
        Name = name,
        Slug = name.ToLowerInvariant().Replace(' ', '-'),
        Affiliation = "University",
        Summary = "Athlete summary",
        PhotoUrl = "/images/athlete.jpg",
        PhotoAlt = $"{name} on the track",
        DisplayOrder = order,
        IsActive = active
    };
}
