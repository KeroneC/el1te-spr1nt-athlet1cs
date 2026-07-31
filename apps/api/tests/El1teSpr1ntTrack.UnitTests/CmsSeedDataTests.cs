using El1teSpr1ntTrack.Infrastructure.Data;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class CmsSeedDataTests
{
    [Fact]
    public void HallOfFameSeeds_PreserveCanonicalProfilesWithoutInferredYears()
    {
        Assert.Collection(CmsSeedData.HallOfFameInductees,
            dani =>
            {
                Assert.Equal("Dani Prunzik", dani.Name);
                Assert.Equal("dani-prunzik", dani.Slug);
                Assert.Null(dani.InductionYear);
                Assert.True(dani.IsActive);
            },
            kaitlyn =>
            {
                Assert.Equal("Kaitlyn Eger", kaitlyn.Name);
                Assert.Equal("kaitlyn-eger", kaitlyn.Slug);
                Assert.Null(kaitlyn.InductionYear);
                Assert.True(kaitlyn.IsActive);
            });
    }

    [Fact]
    public void AboutContent_UsesApprovedMissionAndPreservesValues()
    {
        var mission = Assert.Single(CmsSeedData.ContentBlocks, block => block.Key == "about.story");
        Assert.Equal("Our Mission", mission.Title);
        Assert.Contains("youth ages 7 to 18", mission.Body);
        Assert.Contains("sportsmanship, and discipline", mission.Body);

        var values = Assert.Single(CmsSeedData.ContentBlocks, block => block.Key == "about.values");
        Assert.Equal("What We Value", values.Title);
        Assert.Equal("Effort, respect, teamwork, discipline, and joy guide how we train and compete together.", values.Body);
    }
}
