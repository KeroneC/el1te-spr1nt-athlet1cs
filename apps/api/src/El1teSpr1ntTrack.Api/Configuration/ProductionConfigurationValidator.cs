using El1teSpr1ntTrack.Infrastructure.Security;

namespace El1teSpr1ntTrack.Api.Configuration;

public static class ProductionConfigurationValidator
{
    public static void Validate(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool allowSqlPasswordAuthentication = false)
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

        if (!allowSqlPasswordAuthentication &&
            (!bool.TryParse(configuration["Database:UseManagedIdentity"], out var useManagedIdentity) ||
             !useManagedIdentity))
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

        if (!Uri.TryCreate(configuration["AdminInvitations:SiteUrl"], UriKind.Absolute, out var invitationSiteUrl) ||
            invitationSiteUrl.Scheme != Uri.UriSchemeHttps || invitationSiteUrl.IsLoopback)
        {
            errors.Add("AdminInvitations:SiteUrl must be an absolute HTTPS non-loopback URL in Production.");
        }
        if (!int.TryParse(configuration["AdminInvitations:ExpiresHours"], out var invitationExpiresHours) || invitationExpiresHours is < 1 or > 168)
        {
            errors.Add("AdminInvitations:ExpiresHours must be between 1 and 168.");
        }

        if (configuration.GetValue<bool>("AuthFeatures:AllowPublicRegistration"))
            errors.Add("AuthFeatures:AllowPublicRegistration must remain false until the Parent portal is approved.");
        if (!string.Equals(configuration["TransactionalEmail:Provider"], "AzureCommunicationServices", StringComparison.OrdinalIgnoreCase))
            errors.Add("TransactionalEmail:Provider must be AzureCommunicationServices in Production.");
        Required(configuration, "TransactionalEmail:ConnectionString", errors);
        Required(configuration, "TransactionalEmail:SenderAddress", errors);
        RequiredHttpsUrl(configuration, "TransactionalEmail:AdminSiteUrl", errors);

        if (configuration.GetValue<bool>("Store:Enabled"))
        {
            ValidateCommerce(configuration, errors);
        }
        if (configuration.GetValue<bool>("Printify:Enabled"))
        {
            ValidatePrintify(configuration, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Production configuration is invalid: " + string.Join(" ", errors));
        }
    }

    private static void ValidatePrintify(IConfiguration configuration, ICollection<string> errors)
    {
        Required(configuration, "Printify:AccessToken", errors);
        if (!long.TryParse(configuration["Printify:ShopId"], out var shopId) || shopId <= 0)
            errors.Add("Printify:ShopId must be a positive integer.");
        Required(configuration, "Printify:WebhookSecret", errors);
        RequiredHttpsUrl(configuration, "Printify:WebhookNotificationUrl", errors);
        if (!DateTimeOffset.TryParse(configuration["Printify:TokenExpiresAtUtc"], out var expiresAt) ||
            expiresAt <= DateTimeOffset.UtcNow)
            errors.Add("Printify:TokenExpiresAtUtc must be a future timestamp recorded when the annual token is created.");
        if (!long.TryParse(configuration["Printify:MinimumGrossContributionMinor"], out var floor) || floor < 0)
            errors.Add("Printify:MinimumGrossContributionMinor must be non-negative.");
        if (configuration.GetValue<bool>("Printify:OrderCreationEnabled") &&
            !configuration.GetValue<bool>("Store:CheckoutEnabled"))
            errors.Add("Printify order creation requires Store:CheckoutEnabled.");
        if (configuration.GetValue<bool>("Printify:ProductionReleaseEnabled") &&
            !configuration.GetValue<bool>("Printify:OrderCreationEnabled"))
            errors.Add("Printify production release requires Printify:OrderCreationEnabled.");
    }

    private static void ValidateCommerce(
        IConfiguration configuration,
        ICollection<string> errors)
    {
        if (!string.Equals(configuration["Store:Currency"], "USD", StringComparison.Ordinal))
        {
            errors.Add("Store:Currency must be USD.");
        }

        if (!int.TryParse(configuration["Store:ReservationMinutes"], out var reservationMinutes) ||
            reservationMinutes is < 5 or > 120)
        {
            errors.Add("Store:ReservationMinutes must be between 5 and 120.");
        }

        if (!string.Equals(configuration["Square:Environment"], "Sandbox", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(configuration["Square:Environment"], "Production", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Square:Environment must be Sandbox or Production.");
        }

        Required(configuration, "Square:AccessToken", errors);
        Required(configuration, "Square:LocationId", errors);
        Required(configuration, "Square:WebhookSignatureKey", errors);
        RequiredHttpsUrl(configuration, "Square:WebhookNotificationUrl", errors);
        RequiredHttpsUrl(configuration, "Square:CheckoutReturnUrl", errors);

        if (!DateOnly.TryParseExact(
                configuration["Square:ApiVersion"],
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _))
        {
            errors.Add("Square:ApiVersion must use yyyy-MM-dd format.");
        }
    }

    private static void RequiredHttpsUrl(
        IConfiguration configuration,
        string key,
        ICollection<string> errors)
    {
        if (!Uri.TryCreate(configuration[key], UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.IsLoopback)
        {
            errors.Add($"{key} must be an absolute HTTPS non-loopback URL.");
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
