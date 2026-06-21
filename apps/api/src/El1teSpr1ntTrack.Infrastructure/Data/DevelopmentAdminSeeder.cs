using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Infrastructure.Data;

public sealed class DevelopmentAdminSeeder(
    El1teDbContext dbContext,
    IConfiguration configuration,
    ILogger<DevelopmentAdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];
        var firstName = configuration["SeedAdmin:FirstName"]?.Trim();
        var lastName = configuration["SeedAdmin:LastName"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            logger.LogInformation("Development admin seeding skipped because SeedAdmin settings are incomplete.");
            return;
        }

        if (password.Length < 8)
        {
            logger.LogWarning("Development admin seeding skipped because SeedAdmin:Password is too short.");
            return;
        }

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            logger.LogWarning("Development admin seeding skipped because the database is unavailable.");
            return;
        }

        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            logger.LogInformation("Development admin seeding skipped because the configured user already exists.");
            return;
        }

        dbContext.Users.Add(new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.SuperAdmin,
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development SuperAdmin account created for {Email}.", email);
    }
}
