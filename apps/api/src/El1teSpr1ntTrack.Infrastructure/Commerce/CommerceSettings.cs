namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class StoreSettings
{
    public const string SectionName = "Store";

    public bool Enabled { get; set; }
    public string Currency { get; set; } = "USD";
    public int ReservationMinutes { get; set; } = 30;
    public int DefaultLowStockThreshold { get; set; } = 3;
    public int OutboxPollSeconds { get; set; } = 5;
}

public sealed class SquareSettings
{
    public const string SectionName = "Square";
    public const string SandboxBaseUrl = "https://connect.squareupsandbox.com/";
    public const string ProductionBaseUrl = "https://connect.squareup.com/";

    public string Environment { get; set; } = "Sandbox";
    public string ApiVersion { get; set; } = "2026-07-15";
    public string? AccessToken { get; set; }
    public string? LocationId { get; set; }
    public string? WebhookSignatureKey { get; set; }
    public string? WebhookNotificationUrl { get; set; }
    public string? CheckoutReturnUrl { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 15;

    public string BaseUrl =>
        string.Equals(Environment, "Production", StringComparison.OrdinalIgnoreCase)
            ? ProductionBaseUrl
            : SandboxBaseUrl;
}
