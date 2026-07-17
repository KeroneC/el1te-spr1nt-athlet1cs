using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class AdminIdentityServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateInvitation_StoresOnlyHashAndReturnsFragmentLink()
    {
        var repository = new FakeRepository();
        var actor = SuperAdmin("owner@example.test");
        repository.Users.Add(actor);
        var service = CreateService(repository);

        var result = await service.CreateInvitationAsync(actor.Id,
            new CreateAdminInvitationRequest("New", "Admin", "NEW.ADMIN@example.test", UserRole.Admin), "trace-1", default);

        var invitation = Assert.Single(repository.Invitations);
        Assert.Equal("new.admin@example.test", invitation.Email);
        Assert.Equal(64, invitation.TokenHash.Length);
        Assert.Contains("/admin/invitations/accept#token=", result.InvitationUrl);
        var rawToken = Uri.UnescapeDataString(result.InvitationUrl.Split("#token=")[1]);
        Assert.DoesNotContain(rawToken, invitation.TokenHash);
        Assert.Equal("AdminInvitationCreated", Assert.Single(repository.Activity).Action);
    }

    [Fact]
    public async Task AcceptInvitation_CreatesActivePrivilegedUserAndConsumesInvitation()
    {
        var repository = new FakeRepository();
        var actor = SuperAdmin("owner@example.test");
        repository.Users.Add(actor);
        var service = CreateService(repository);
        var created = await service.CreateInvitationAsync(actor.Id,
            new CreateAdminInvitationRequest("New", "Leader", "leader@example.test", UserRole.SuperAdmin), null, default);
        var token = Uri.UnescapeDataString(created.InvitationUrl.Split("#token=")[1]);

        await service.AcceptInvitationAsync(new AcceptAdminInvitationRequest(token, "StrongAdmin!2026", "StrongAdmin!2026"), "trace-2", default);

        var user = Assert.Single(repository.Users, value => value.Email == "leader@example.test");
        Assert.Equal(UserRole.SuperAdmin, user.Role);
        Assert.True(user.IsActive);
        Assert.True(BCrypt.Net.BCrypt.Verify("StrongAdmin!2026", user.PasswordHash));
        Assert.NotNull(Assert.Single(repository.Invitations).AcceptedAt);
        Assert.Contains(repository.Activity, value => value.Action == "AdminInvitationAccepted" && value.ActorUserId == user.Id);
        await Assert.ThrowsAsync<CmsConflictException>(() => service.InspectInvitationAsync(new InspectAdminInvitationRequest(token), default));
    }

    [Fact]
    public async Task AcceptInvitation_RejectsExcessivelyLongPassword()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() => service.AcceptInvitationAsync(
            new AcceptAdminInvitationRequest("valid-token-value-with-enough-characters", $"A1!{new string('a', 126)}", $"A1!{new string('a', 126)}"),
            null,
            default));

        Assert.Contains(nameof(AcceptAdminInvitationRequest.Password), exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateUser_RejectsChangesToCurrentAccount()
    {
        var repository = new FakeRepository();
        var actor = SuperAdmin("owner@example.test");
        repository.Users.Add(actor);

        await Assert.ThrowsAsync<CmsConflictException>(() => CreateService(repository)
            .UpdateUserAsync(actor.Id, actor.Id, new UpdateAdminUserRequest(UserRole.Admin, true), null, default));
    }

    [Fact]
    public async Task UpdateUser_ProtectsFinalActiveSuperAdmin()
    {
        var repository = new FakeRepository();
        var actor = SuperAdmin("owner@example.test");
        var finalSuperAdmin = SuperAdmin("final@example.test");
        actor.IsActive = false;
        repository.Users.AddRange([actor, finalSuperAdmin]);

        await Assert.ThrowsAsync<CmsConflictException>(() => CreateService(repository)
            .UpdateUserAsync(actor.Id, finalSuperAdmin.Id, new UpdateAdminUserRequest(UserRole.Admin, true), null, default));
    }

    [Fact]
    public async Task UpdateUser_RecordsSafeRoleAndStatusAudit()
    {
        var repository = new FakeRepository();
        var actor = SuperAdmin("owner@example.test");
        var target = new User { FirstName = "Alex", LastName = "Admin", Email = "alex@example.test", Role = UserRole.Admin, IsActive = true };
        repository.Users.AddRange([actor, target]);

        var result = await CreateService(repository).UpdateUserAsync(actor.Id, target.Id,
            new UpdateAdminUserRequest(UserRole.SuperAdmin, false), "trace-3", default);

        Assert.Equal(UserRole.SuperAdmin, result.Role);
        Assert.False(result.IsActive);
        var activity = Assert.Single(repository.Activity);
        Assert.Equal("AdminUserUpdated", activity.Action);
        Assert.Contains("role Admin to SuperAdmin", activity.Summary);
        Assert.Contains("status active to inactive", activity.Summary);
    }

    private static AdminIdentityService CreateService(FakeRepository repository) => new(
        repository,
        new FakeClock(),
        new AdminInvitationSettings { SiteUrl = "https://club.example.test", ExpiresHours = 72 });

    private static User SuperAdmin(string email) => new()
    {
        FirstName = "Super", LastName = "Admin", Email = email, PasswordHash = "hash", Role = UserRole.SuperAdmin, IsActive = true
    };

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeRepository : IAdminIdentityRepository
    {
        public List<User> Users { get; } = [];
        public List<AdminInvitation> Invitations { get; } = [];
        public List<AdminActivityLog> Activity { get; } = [];

        public Task<AdminPage<User>> GetUsersAsync(AdminUserOptions options, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminPage<User>(Users.Where(IsPrivileged).ToList(), Users.Count(IsPrivileged)));

        public Task<User?> GetPrivilegedUserAsync(Guid id, bool tracked, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(value => value.Id == id && IsPrivileged(value)));

        public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(value => value.Email == email));

        public Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Users.Count(value => value.Role == UserRole.SuperAdmin && value.IsActive));

        public Task<AdminPage<AdminInvitation>> GetInvitationsAsync(AdminInvitationOptions options, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminPage<AdminInvitation>(Invitations, Invitations.Count));

        public Task<AdminInvitation?> GetInvitationAsync(Guid id, bool tracked, CancellationToken cancellationToken)
        {
            var value = Invitations.FirstOrDefault(invitation => invitation.Id == id);
            if (value is not null) value.InvitedByUser = Users.FirstOrDefault(user => user.Id == value.InvitedByUserId);
            return Task.FromResult(value);
        }

        public Task<AdminInvitation?> GetInvitationByTokenHashAsync(string tokenHash, bool tracked, CancellationToken cancellationToken) =>
            Task.FromResult(Invitations.FirstOrDefault(value => value.TokenHash == tokenHash));

        public Task<bool> HasPendingInvitationAsync(string email, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(Invitations.Any(value => value.Email == email && value.AcceptedAt == null && value.RevokedAt == null && value.ExpiresAt > now));

        public Task<AdminPage<AdminActivityLog>> GetActivityAsync(AdminActivityOptions options, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminPage<AdminActivityLog>(Activity, Activity.Count));

        public Task AddInvitationAsync(AdminInvitation invitation, CancellationToken cancellationToken) { Invitations.Add(invitation); return Task.CompletedTask; }
        public Task AddUserAsync(User user, CancellationToken cancellationToken) { Users.Add(user); return Task.CompletedTask; }
        public Task AddActivityAsync(AdminActivityLog activity, CancellationToken cancellationToken) { Activity.Add(activity); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteSerializableAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken);
        private static bool IsPrivileged(User user) => user.Role is UserRole.Admin or UserRole.SuperAdmin;
    }
}
