using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class ProductionAdminBootstrapperTests
{
    [Fact]
    public async Task Bootstrap_CreatesOneSuperAdminAndIsIdempotentForSameEmail()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"bootstrap-{Guid.NewGuid():N}").Options;
        await using var db = new El1teDbContext(options);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Email"] = "admin@example.org",
            ["BootstrapAdmin:Password"] = "StrongBootstrapPassword123!",
            ["BootstrapAdmin:FirstName"] = "Demo",
            ["BootstrapAdmin:LastName"] = "Admin"
        }).Build();
        var bootstrapper = new ProductionAdminBootstrapper(db, configuration);

        Assert.True(await bootstrapper.RunAsync());
        Assert.False(await bootstrapper.RunAsync());
        var admin = Assert.Single(await db.Users.ToListAsync());
        Assert.Equal(UserRole.SuperAdmin, admin.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("StrongBootstrapPassword123!", admin.PasswordHash));
    }
}
