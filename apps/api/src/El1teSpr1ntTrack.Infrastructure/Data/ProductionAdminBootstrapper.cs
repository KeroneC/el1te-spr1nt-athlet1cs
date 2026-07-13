using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace El1teSpr1ntTrack.Infrastructure.Data;

public sealed class ProductionAdminBootstrapper(El1teDbContext dbContext, IConfiguration configuration)
{
    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        var email = Required("BootstrapAdmin:Email").Trim().ToLowerInvariant();
        var password = Required("BootstrapAdmin:Password");
        var firstName = Required("BootstrapAdmin:FirstName").Trim();
        var lastName = Required("BootstrapAdmin:LastName").Trim();
        if (password.Length < 12) throw new InvalidOperationException("BootstrapAdmin:Password must be at least 12 characters.");

        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken)) return false;
        if (await dbContext.Users.AnyAsync(user => user.Role == UserRole.SuperAdmin, cancellationToken))
            throw new InvalidOperationException("A SuperAdmin already exists; bootstrap refused.");

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
        return true;
    }

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required for admin bootstrap.")
        : configuration[key]!;
}
