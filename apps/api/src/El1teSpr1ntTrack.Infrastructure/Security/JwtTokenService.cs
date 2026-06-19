using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace El1teSpr1ntTrack.Infrastructure.Security;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public AuthTokenResult GenerateToken(User user)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is required.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is required.");
        var expiresMinutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var configuredMinutes)
            ? configuredMinutes
            : 60;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresMinutes);
        var signingKey = JwtSecurityKeyFactory.Create(configuration["Jwt:Key"]);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim())
        };

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
