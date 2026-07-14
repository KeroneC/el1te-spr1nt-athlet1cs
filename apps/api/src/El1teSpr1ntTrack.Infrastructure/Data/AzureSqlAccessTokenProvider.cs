using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace El1teSpr1ntTrack.Infrastructure.Data;

public static class AzureSqlAccessTokenProvider
{
    private const string DefaultScopeSuffix = "/.default";
    private static readonly ManagedIdentityCredential Credential = new(new ManagedIdentityCredentialOptions());

    public static Func<SqlAuthenticationParameters, CancellationToken, Task<SqlAuthenticationToken>> Callback { get; } =
        GetTokenAsync;

    private static async Task<SqlAuthenticationToken> GetTokenAsync(
        SqlAuthenticationParameters parameters,
        CancellationToken cancellationToken)
    {
        var scope = BuildScope(parameters.Resource);
        var token = await Credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken);

        return new SqlAuthenticationToken(token.Token, token.ExpiresOn);
    }

    internal static string BuildScope(string resource)
    {
        return resource.EndsWith(DefaultScopeSuffix, StringComparison.Ordinal)
            ? resource
            : $"{resource.TrimEnd('/')}{DefaultScopeSuffix}";
    }
}
