using System.Text.RegularExpressions;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class AdminAuthenticationHardeningTests
{
    [Fact]
    public async Task SuperAdminLogin_RequiresOneTimeEmailCode()
    {
        await using var fixture = await Fixture.CreateAsync(UserRole.SuperAdmin);
        var login = await fixture.Service.LoginAsync(new AdminLoginRequestDto { Email = "admin@example.com", Password = "Strong-Password-42!" }, "127.0.0.1", default);
        Assert.True(login.RequiresMfa);
        Assert.Null(login.Authentication);
        var code = Regex.Match(fixture.Email.Messages.Single().PlainText, @"\b\d{6}\b").Value;

        var response = await fixture.Service.VerifyMfaAsync(new AdminMfaVerifyRequestDto { ChallengeToken = login.ChallengeToken!, Code = code }, "127.0.0.1", default);
        Assert.Equal("token", response.AccessToken);
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => fixture.Service.VerifyMfaAsync(new AdminMfaVerifyRequestDto { ChallengeToken = login.ChallengeToken!, Code = code }, "127.0.0.1", default));
    }

    [Fact]
    public async Task FiveFailedPasswords_LockAccountForConfiguredWindow()
    {
        await using var fixture = await Fixture.CreateAsync(UserRole.Admin);
        for (var attempt = 0; attempt < 5; attempt++)
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => fixture.Service.LoginAsync(new AdminLoginRequestDto { Email = "admin@example.com", Password = "wrong" }, $"ip-{attempt}", default));
        var user = await fixture.Db.Users.SingleAsync();
        Assert.NotNull(user.LockoutEndUtc);
        Assert.True(user.LockoutEndUtc > fixture.Clock.UtcNow);
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => fixture.Service.LoginAsync(new AdminLoginRequestDto { Email = "admin@example.com", Password = "Strong-Password-42!" }, "new-ip", default));
    }

    [Fact]
    public async Task PasswordReset_IsGenericOneTimeAndRevokesSessions()
    {
        await using var fixture = await Fixture.CreateAsync(UserRole.Admin);
        await fixture.Service.RequestPasswordResetAsync(new AdminPasswordResetRequestDto { Email = "missing@example.com" }, "unknown-ip", default);
        Assert.Empty(fixture.Email.Messages);
        await fixture.Service.RequestPasswordResetAsync(new AdminPasswordResetRequestDto { Email = "admin@example.com" }, "known-ip", default);
        var token = Regex.Match(fixture.Email.Messages.Single().PlainText, @"token=([^\s]+)").Groups[1].Value;
        Assert.True((await fixture.Service.InspectPasswordResetAsync(token, default)).IsValid);
        await fixture.Service.CompletePasswordResetAsync(new AdminPasswordResetCompleteDto { Token = token, Password = "New-Password-42!", ConfirmPassword = "New-Password-42!" }, "complete-ip", default);
        Assert.False((await fixture.Service.InspectPasswordResetAsync(token, default)).IsValid);
        Assert.Equal(2, (await fixture.Db.Users.SingleAsync()).SecurityVersion);
    }

    [Fact]
    public async Task PasswordReset_EmailFailure_RemainsGenericAndRevokesGeneratedLink()
    {
        await using var fixture = await Fixture.CreateAsync(UserRole.Admin);
        fixture.Email.ThrowOnSend = true;
        await fixture.Service.RequestPasswordResetAsync(new AdminPasswordResetRequestDto { Email = "admin@example.com" }, "known-ip", default);
        Assert.All(await fixture.Db.AdminPasswordResets.ToListAsync(), value => Assert.NotNull(value.RevokedAtUtc));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required El1teDbContext Db { get; init; }
        public required AdminAuthenticationService Service { get; init; }
        public required CaptureEmail Email { get; init; }
        public required TestClock Clock { get; init; }
        public static async Task<Fixture> CreateAsync(UserRole role)
        {
            var db = new El1teDbContext(new DbContextOptionsBuilder<El1teDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            db.Users.Add(new User { Email = "admin@example.com", FirstName = "Admin", LastName = "User", Role = role, IsActive = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong-Password-42!") });
            await db.SaveChangesAsync();
            var email = new CaptureEmail(); var clock = new TestClock();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "test-key-that-is-at-least-thirty-two-bytes-long" }).Build();
            return new Fixture
            {
                Db = db, Email = email, Clock = clock,
                Service = new AdminAuthenticationService(db, new TestJwt(), email, clock, new AuthFeatureSettings(),
                    new TransactionalEmailSettings { AdminSiteUrl = "https://example.test" }, configuration,
                    NullLogger<AdminAuthenticationService>.Instance)
            };
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class CaptureEmail : ITransactionalEmailSender
    {
        public List<TransactionalEmail> Messages { get; } = [];
        public bool ThrowOnSend { get; set; }
        public Task<TransactionalEmailSendResult> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
        {
            if (ThrowOnSend) throw new InvalidOperationException("Simulated delivery failure.");
            Messages.Add(message); return Task.FromResult(new TransactionalEmailSendResult("provider-message-1"));
        }
    }
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow { get; } = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero); }
    private sealed class TestJwt : IJwtTokenService { public AuthTokenResult GenerateToken(User user) => new("token", DateTimeOffset.UtcNow.AddHours(1)); }
}
