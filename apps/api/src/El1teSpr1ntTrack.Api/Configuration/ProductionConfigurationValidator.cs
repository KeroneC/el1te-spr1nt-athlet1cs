using El1teSpr1ntTrack.Infrastructure.Security;

namespace El1teSpr1ntTrack.Api.Configuration;

public static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var errors = new List<string>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:DefaultConnection is required.");
        }
        else if (connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("ConnectionStrings:DefaultConnection must not use LocalDB in Production.");
        }

        if (!bool.TryParse(configuration["Database:UseManagedIdentity"], out var useManagedIdentity) ||
            !useManagedIdentity)
        {
            errors.Add("Database:UseManagedIdentity must be true in Production.");
        }

        Required(configuration, "Jwt:Issuer", errors);
        Required(configuration, "Jwt:Audience", errors);

        if (!int.TryParse(configuration["Jwt:ExpiresMinutes"], out var expiresMinutes) || expiresMinutes <= 0)
        {
            errors.Add("Jwt:ExpiresMinutes must be a positive integer.");
        }

        try
        {
            JwtSecurityKeyFactory.Create(configuration["Jwt:Key"]);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0)
        {
            errors.Add("Cors:AllowedOrigins must contain at least one Production origin.");
        }
        else if (allowedOrigins.Any(origin =>
                     !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                     uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback))
        {
            errors.Add("Cors:AllowedOrigins must contain only absolute HTTPS non-loopback origins in Production.");
        }

        if (!string.Equals(configuration["MediaStorage:Provider"], "AzureBlob", StringComparison.OrdinalIgnoreCase))
            errors.Add("MediaStorage:Provider must be AzureBlob in Production.");
        if (!Uri.TryCreate(configuration["MediaStorage:BlobServiceUri"], UriKind.Absolute, out var blobServiceUri) ||
            blobServiceUri.Scheme != Uri.UriSchemeHttps || blobServiceUri.IsLoopback)
            errors.Add("MediaStorage:BlobServiceUri must be an absolute HTTPS non-loopback URL in Production.");
        Required(configuration, "MediaStorage:ContainerName", errors);
        if (!long.TryParse(configuration["MediaStorage:MaxFileSizeBytes"], out var maxFileSize) || maxFileSize <= 0)
        {
            errors.Add("MediaStorage:MaxFileSizeBytes must be a positive integer.");
        }
        if (!Uri.TryCreate(configuration["MediaStorage:PublicBaseUrl"], UriKind.Absolute, out var mediaBaseUrl) ||
            mediaBaseUrl.Scheme != Uri.UriSchemeHttps || mediaBaseUrl.IsLoopback)
        {
            errors.Add("MediaStorage:PublicBaseUrl must be an absolute HTTPS non-loopback URL in Production.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Production configuration is invalid: " + string.Join(" ", errors));
        }
    }

    private static void Required(IConfiguration configuration, string key, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
        {
            errors.Add($"{key} is required.");
        }
    }
}
