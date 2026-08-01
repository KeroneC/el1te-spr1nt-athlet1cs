using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Infrastructure.Security;

public sealed class AdminAuthenticationService(
    El1teDbContext dbContext,
    IJwtTokenService jwtTokenService,
    ITransactionalEmailSender emailSender,
    IClock clock,
    AuthFeatureSettings settings,
    TransactionalEmailSettings emailSettings,
    IConfiguration configuration,
    ILogger<AdminAuthenticationService> logger) : IAdminAuthenticationService
{
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("TimingOnly-Password-42!");
    private readonly byte[] _codePepper = Encoding.UTF8.GetBytes(
        configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required."));

    public async Task<AdminLoginResultDto> LoginAsync(
        AdminLoginRequestDto request,
        string clientPartition,
        CancellationToken cancellationToken)
    {
        await EnsurePartitionAvailableAsync("admin-login", clientPartition, 20, cancellationToken);
        var email = NormalizeEmail(request.Email);
        await EnsurePartitionAvailableAsync("admin-login-account", email, 10, cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(value => value.Email == email, cancellationToken);
        var now = clock.UtcNow;

        if (user is null || user.Role is not (UserRole.Admin or UserRole.SuperAdmin))
        {
            BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, DummyPasswordHash);
            await RecordAttemptAsync("admin-login", clientPartition, false, cancellationToken);
            await RecordAttemptAsync("admin-login-account", email, false, cancellationToken);
            throw new InvalidCredentialsException();
        }

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user.PasswordHash);
        var isLocked = user.LockoutEndUtc > now;
        if (!user.IsActive || isLocked || !passwordIsValid)
        {
            if (user.IsActive && !isLocked) RegisterFailedLogin(user, now);
            await RecordAttemptAsync("admin-login", clientPartition, false, cancellationToken);
            await RecordAttemptAsync("admin-login-account", email, false, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        user.FailedLoginCount = 0;
        user.FailedLoginWindowStartedUtc = null;
        user.LockoutEndUtc = null;
        user.LastSuccessfulLoginUtc = now;
        user.UpdatedAt = now;
        await RecordAttemptAsync("admin-login", clientPartition, true, cancellationToken);
        await RecordAttemptAsync("admin-login-account", email, true, cancellationToken);

        if (user.Role == UserRole.SuperAdmin)
        {
            foreach (var prior in await dbContext.AdminMfaChallenges
                         .Where(value => value.UserId == user.Id && value.UsedAtUtc == null && value.ExpiresAtUtc > now)
                         .ToListAsync(cancellationToken))
            {
                prior.UsedAtUtc = now;
                prior.UpdatedAt = now;
            }

            var challengeToken = Token();
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var expires = now.AddMinutes(Math.Clamp(settings.MfaMinutes, 5, 30));
            dbContext.AdminMfaChallenges.Add(new AdminMfaChallenge
            {
                UserId = user.Id,
                ChallengeTokenHash = Hash(challengeToken),
                CodeHash = CodeHash(challengeToken, code),
                ExpiresAtUtc = expires,
                CreatedAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            await emailSender.SendAsync(new TransactionalEmail(
                user.Email,
                "Your El1te Admin verification code",
                $"Your verification code is {code}. It expires in {settings.MfaMinutes} minutes. If you did not try to sign in, contact the club.",
                $"<p>Your El1te Admin verification code is <strong>{code}</strong>.</p><p>It expires in {settings.MfaMinutes} minutes. If you did not try to sign in, contact the club.</p>"), cancellationToken);

            return new AdminLoginResultDto(true, null, challengeToken, expires);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdminLoginResultDto(false, CreateResponse(user), null, null);
    }

    public async Task<AuthResponseDto> VerifyMfaAsync(
        AdminMfaVerifyRequestDto request,
        string clientPartition,
        CancellationToken cancellationToken)
    {
        await EnsurePartitionAvailableAsync("admin-mfa", clientPartition, 20, cancellationToken);
        var now = clock.UtcNow;
        var challenge = await dbContext.AdminMfaChallenges
            .Include(value => value.User)
            .FirstOrDefaultAsync(value => value.ChallengeTokenHash == Hash(request.ChallengeToken), cancellationToken);
        var valid = challenge is not null && challenge.UsedAtUtc is null && challenge.ExpiresAtUtc > now &&
                    challenge.FailedAttemptCount < Math.Clamp(settings.MfaMaximumAttempts, 3, 10) &&
                    challenge.User.IsActive && challenge.User.Role == UserRole.SuperAdmin &&
                    FixedEquals(challenge.CodeHash, CodeHash(request.ChallengeToken, request.Code));
        if (!valid)
        {
            if (challenge is not null)
            {
                challenge.FailedAttemptCount++;
                challenge.UpdatedAt = now;
            }
            await RecordAttemptAsync("admin-mfa", clientPartition, false, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        challenge!.UsedAtUtc = now;
        challenge.UpdatedAt = now;
        challenge.User.LastSuccessfulLoginUtc = now;
        challenge.User.UpdatedAt = now;
        await RecordAttemptAsync("admin-mfa", clientPartition, true, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateResponse(challenge.User);
    }

    public async Task RequestPasswordResetAsync(
        AdminPasswordResetRequestDto request,
        string clientPartition,
        CancellationToken cancellationToken)
    {
        await EnsurePartitionAvailableAsync("admin-password-reset", clientPartition, 5, cancellationToken);
        var email = NormalizeEmail(request.Email);
        await EnsurePartitionAvailableAsync("admin-password-reset-account", email, 5, cancellationToken);
        await RecordAttemptAsync("admin-password-reset", clientPartition, true, cancellationToken);
        await RecordAttemptAsync("admin-password-reset-account", email, true, cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(value =>
            value.Email == email && value.IsActive &&
            (value.Role == UserRole.Admin || value.Role == UserRole.SuperAdmin), cancellationToken);
        if (user is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var now = clock.UtcNow;
        foreach (var prior in await dbContext.AdminPasswordResets
                     .Where(value => value.UserId == user.Id && value.UsedAtUtc == null && value.RevokedAtUtc == null)
                     .ToListAsync(cancellationToken))
        {
            prior.RevokedAtUtc = now;
            prior.UpdatedAt = now;
        }

        var token = Token();
        var reset = new AdminPasswordReset
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAtUtc = now.AddMinutes(Math.Clamp(settings.PasswordResetMinutes, 15, 60)),
            CreatedAt = now
        };
        dbContext.AdminPasswordResets.Add(reset);
        await dbContext.SaveChangesAsync(cancellationToken);

        var url = $"{emailSettings.AdminSiteUrl.TrimEnd('/')}/admin/password-reset#token={Uri.EscapeDataString(token)}";
        try
        {
            await emailSender.SendAsync(new TransactionalEmail(
                user.Email,
                "Reset your El1te Admin password",
                $"Open this one-time link within {settings.PasswordResetMinutes} minutes: {url}",
                $"<p>Use the one-time link below within {settings.PasswordResetMinutes} minutes.</p><p><a href=\"{WebUtility.HtmlEncode(url)}\">Reset Admin password</a></p><p>If you did not request this, no action is required.</p>"), cancellationToken);
        }
        catch (Exception exception)
        {
            reset.RevokedAtUtc = clock.UtcNow;
            reset.UpdatedAt = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning(exception, "Admin password recovery email delivery failed for request {PasswordResetId}.", reset.Id);
        }
    }

    public async Task<AdminPasswordResetInspectionDto> InspectPasswordResetAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (!ValidToken(token)) return new AdminPasswordResetInspectionDto(false);
        var now = clock.UtcNow;
        var valid = await dbContext.AdminPasswordResets.AsNoTracking().AnyAsync(value =>
            value.TokenHash == Hash(token) && value.UsedAtUtc == null && value.RevokedAtUtc == null &&
            value.ExpiresAtUtc > now && value.User.IsActive &&
            (value.User.Role == UserRole.Admin || value.User.Role == UserRole.SuperAdmin), cancellationToken);
        return new AdminPasswordResetInspectionDto(valid);
    }

    public async Task CompletePasswordResetAsync(
        AdminPasswordResetCompleteDto request,
        string clientPartition,
        CancellationToken cancellationToken)
    {
        await EnsurePartitionAvailableAsync("admin-password-complete", clientPartition, 10, cancellationToken);
        ValidatePassword(request.Password, request.ConfirmPassword);
        var now = clock.UtcNow;
        var reset = ValidToken(request.Token)
            ? await dbContext.AdminPasswordResets.Include(value => value.User)
                .FirstOrDefaultAsync(value => value.TokenHash == Hash(request.Token), cancellationToken)
            : null;
        if (reset is null || reset.UsedAtUtc is not null || reset.RevokedAtUtc is not null ||
            reset.ExpiresAtUtc <= now || !reset.User.IsActive ||
            reset.User.Role is not (UserRole.Admin or UserRole.SuperAdmin))
        {
            throw new CmsRequestValidationException(new Dictionary<string, string[]>
            {
                ["Token"] = ["This password reset link is invalid or expired."]
            });
        }

        reset.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        reset.User.SecurityVersion++;
        reset.User.FailedLoginCount = 0;
        reset.User.FailedLoginWindowStartedUtc = null;
        reset.User.LockoutEndUtc = null;
        reset.User.UpdatedAt = now;
        reset.UsedAtUtc = now;
        reset.UpdatedAt = now;
        foreach (var challenge in await dbContext.AdminMfaChallenges
                     .Where(value => value.UserId == reset.UserId && value.UsedAtUtc == null)
                     .ToListAsync(cancellationToken))
        {
            challenge.UsedAtUtc = now;
            challenge.UpdatedAt = now;
        }
        await RecordAttemptAsync("admin-password-complete", clientPartition, true, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void RegisterFailedLogin(User user, DateTimeOffset now)
    {
        var window = TimeSpan.FromMinutes(Math.Clamp(settings.FailedLoginWindowMinutes, 5, 60));
        if (user.FailedLoginWindowStartedUtc is null || user.FailedLoginWindowStartedUtc < now.Subtract(window))
        {
            user.FailedLoginWindowStartedUtc = now;
            user.FailedLoginCount = 1;
        }
        else
        {
            user.FailedLoginCount++;
        }

        if (user.FailedLoginCount >= Math.Clamp(settings.FailedLoginLimit, 3, 10))
        {
            user.LockoutEndUtc = now.AddMinutes(Math.Clamp(settings.LockoutMinutes, 5, 60));
        }
        user.UpdatedAt = now;
    }

    private async Task EnsurePartitionAvailableAsync(string purpose, string partition, int maximum, CancellationToken token)
    {
        var partitionHash = Hash(partition);
        var since = clock.UtcNow.AddMinutes(-15);
        if (await dbContext.AuthenticationAttempts.CountAsync(value =>
                value.Purpose == purpose && value.PartitionHash == partitionHash && value.CreatedAt >= since, token) >= maximum)
        {
            throw new TooManyAttemptsException();
        }
    }

    private async Task RecordAttemptAsync(string purpose, string partition, bool succeeded, CancellationToken token)
    {
        await dbContext.AuthenticationAttempts.AddAsync(new AuthenticationAttempt
        {
            Purpose = purpose,
            PartitionHash = Hash(partition),
            WasSuccessful = succeeded,
            CreatedAt = clock.UtcNow
        }, token);
    }

    private AuthResponseDto CreateResponse(User user)
    {
        var token = jwtTokenService.GenerateToken(user);
        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = new UserSummaryDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    private static void ValidatePassword(string password, string confirmation)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) ||
            !password.Any(value => !char.IsLetterOrDigit(value)))
        {
            errors["Password"] = ["Password must be 12 to 128 characters and include uppercase, lowercase, number, and symbol characters."];
        }
        if (password != confirmation) errors["ConfirmPassword"] = ["Password and confirmation password do not match."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private static string NormalizeEmail(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
    private static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool ValidToken(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length is >= 40 and <= 200;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private string CodeHash(string token, string code)
    {
        using var hmac = new HMACSHA256(_codePepper);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{token}:{code}")));
    }
    private static bool FixedEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left), Convert.FromHexString(right));
}
