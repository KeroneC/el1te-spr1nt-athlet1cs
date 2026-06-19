using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace El1teSpr1ntTrack.Infrastructure.Security;

public static class JwtSecurityKeyFactory
{
    public const int MinimumKeyLength = 32;

    public static SymmetricSecurityKey Create(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Jwt:Key is required. Set it with .NET user secrets or an environment variable.");
        }

        if (key.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException($"Jwt:Key must be at least {MinimumKeyLength} characters long.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
