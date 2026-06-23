using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Api.Controllers;
using El1teSpr1ntTrack.Api.Controllers.Admin;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class AdminAuthorizationTests
{
    [Theory]
    [InlineData(UserRole.Admin, true, true)]
    [InlineData(UserRole.SuperAdmin, true, true)]
    [InlineData(UserRole.Parent, true, false)]
    [InlineData(UserRole.Admin, false, false)]
    public async Task CmsAdminPolicy_RequiresPrivilegedRoleAndActiveDatabaseUser(
        UserRole role,
        bool active,
        bool expected)
    {
        var user = new User { Role = role, IsActive = active, Email = "user@example.com" };
        var repository = new FakeUserRepository(user);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore(CmsAdminAuthorization.Configure);
        services.AddSingleton<IAuthorizationHandler>(new ActiveCmsAdminHandler(repository));
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = Principal(user);

        var result = await authorization.AuthorizeAsync(principal, null, CmsAdminAuthorization.PolicyName);

        Assert.Equal(expected, result.Succeeded);
    }

    [Fact]
    public async Task CmsAdminPolicy_RejectsUnauthenticatedCaller()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore(CmsAdminAuthorization.Configure);
        services.AddSingleton<IAuthorizationHandler>(new ActiveCmsAdminHandler(new FakeUserRepository(null)));
        await using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, CmsAdminAuthorization.PolicyName);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void EveryAdminController_RequiresCmsAdminPolicy()
    {
        Type[] controllerTypes =
        [
            typeof(AdminSiteSettingsController), typeof(AdminContentBlocksController),
            typeof(AdminAnnouncementsController), typeof(AdminEventsController),
            typeof(AdminCoachesController), typeof(AdminSponsorsController),
            typeof(AdminFaqsController), typeof(AdminContactSubmissionsController),
            typeof(AdminMediaController), typeof(AdminGalleryAlbumsController)
        ];

        foreach (var type in controllerTypes)
        {
            var attribute = Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
            Assert.Equal(CmsAdminAuthorization.PolicyName, attribute.Policy);
        }
    }

    [Fact]
    public void CurrentUserEndpoint_RequiresAuthentication_AndPublicCmsDoesNot()
    {
        var me = typeof(AuthController).GetMethod(nameof(AuthController.Me));
        Assert.NotNull(me);
        Assert.Single(me.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(typeof(PublicCmsController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    private static ClaimsPrincipal Principal(User user) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    ], "Test"));

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Id == id ? user : null);
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
