using El1teSpr1ntTrack.Infrastructure.Data;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class AzureSqlAccessTokenProviderTests
{
    [Theory]
    [InlineData("https://database.windows.net/", "https://database.windows.net/.default")]
    [InlineData("https://database.windows.net", "https://database.windows.net/.default")]
    [InlineData("https://database.windows.net/.default", "https://database.windows.net/.default")]
    public void BuildScope_NormalizesSqlResource(string resource, string expected)
    {
        Assert.Equal(expected, AzureSqlAccessTokenProvider.BuildScope(resource));
    }
}
