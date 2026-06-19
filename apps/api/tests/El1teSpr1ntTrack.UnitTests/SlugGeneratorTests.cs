using El1teSpr1ntTrack.Application.Services;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class SlugGeneratorTests
{
    private readonly SlugGenerator _generator = new();

    [Fact]
    public void Generate_ConvertsTitleToUrlFriendlySlug()
    {
        var slug = _generator.Generate("Summer Track Registration Opens!");

        Assert.Equal("summer-track-registration-opens", slug);
    }

    [Theory]
    [InlineData("Practice   Schedule---Update", "practice-schedule-update")]
    [InlineData("Café & Community Meet", "cafe-community-meet")]
    [InlineData("  Gold Sponsor  ", "gold-sponsor")]
    public void Generate_NormalizesAnnouncementAndSponsorTitles(string title, string expected)
    {
        Assert.Equal(expected, _generator.Generate(title));
    }

    [Fact]
    public async Task GenerateUniqueAsync_AppendsNextAvailableNumber()
    {
        var existing = new HashSet<string>(StringComparer.Ordinal)
        {
            "summer-track-registration-opens",
            "summer-track-registration-opens-2"
        };

        var slug = await _generator.GenerateUniqueAsync(
            "Summer Track Registration Opens!",
            (candidate, _) => Task.FromResult(existing.Contains(candidate)));

        Assert.Equal("summer-track-registration-opens-3", slug);
    }
}
