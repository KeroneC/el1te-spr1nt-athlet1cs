using System.Text.Json;
using El1teSpr1ntTrack.Api.Configuration;
using El1teSpr1ntTrack.Api.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class DeploymentReadinessTests
{
    [Fact]
    public void ProductionConfiguration_RejectsUnsafeOrMissingValues()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Local;",
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience",
            ["Jwt:ExpiresMinutes"] = "60",
            ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
        }).Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, new TestEnvironment("Production")));

        Assert.Contains("LocalDB", exception.Message);
        Assert.Contains("Database:UseManagedIdentity", exception.Message);
        Assert.Contains("Jwt:Key", exception.Message);
        Assert.Contains("HTTPS non-loopback", exception.Message);
    }

    [Fact]
    public void ProductionConfiguration_AcceptsRequiredSafeValues()
    {
        var configuration = BuildSafeProductionConfiguration(useManagedIdentity: true);

        ProductionConfigurationValidator.Validate(configuration, new TestEnvironment("Production"));
    }

    [Fact]
    public void ProductionConfiguration_AllowsSqlPasswordOnlyForExplicitBootstrapCommand()
    {
        var configuration = BuildSafeProductionConfiguration(useManagedIdentity: false);

        ProductionConfigurationValidator.Validate(
            configuration,
            new TestEnvironment("Production"),
            allowSqlPasswordAuthentication: true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, new TestEnvironment("Production")));
        Assert.Contains("Database:UseManagedIdentity", exception.Message);
    }

    [Fact]
    public void ProductionConfiguration_RequiresSquareSecretsOnlyWhenStoreIsEnabled()
    {
        var values = SafeProductionValues(useManagedIdentity: true);
        values["Store:Enabled"] = "true";
        values["Store:Currency"] = "USD";
        values["Store:ReservationMinutes"] = "30";
        values["Square:Environment"] = "Production";
        values["Square:ApiVersion"] = "2026-07-15";
        var missingSquare = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(missingSquare, new TestEnvironment("Production")));
        Assert.Contains("Square:AccessToken", exception.Message);
        Assert.Contains("Square:WebhookNotificationUrl", exception.Message);

        values["Square:AccessToken"] = "secret-token";
        values["Square:LocationId"] = "location";
        values["Square:WebhookSignatureKey"] = "signature-secret";
        values["Square:WebhookNotificationUrl"] = "https://api.example.invalid/api/webhooks/square";
        values["Square:CheckoutReturnUrl"] = "https://web.example.invalid/shop/order-confirmation";
        var configuredSquare = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        ProductionConfigurationValidator.Validate(configuredSquare, new TestEnvironment("Production"));
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, "healthy")]
    [InlineData(HealthStatus.Unhealthy, "unhealthy")]
    public async Task HealthResponse_ContainsOnlySafeStatus(HealthStatus healthStatus, string expected)
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(
                    healthStatus,
                    "Server=secret;Password=secret",
                    TimeSpan.Zero,
                    new Exception("sensitive internal failure"),
                    new Dictionary<string, object>())
            },
            TimeSpan.Zero);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await SafeHealthResponseWriter.WriteAsync(context, report);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(expected, document.RootElement.GetProperty("status").GetString());
        Assert.Single(document.RootElement.EnumerateObject());
    }

    private static IConfiguration BuildSafeProductionConfiguration(bool useManagedIdentity)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(SafeProductionValues(useManagedIdentity))
            .Build();
    }

    private static Dictionary<string, string?> SafeProductionValues(bool useManagedIdentity)
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=tcp:sql.example.invalid,1433;Initial Catalog=app;Encrypt=True;",
            ["Database:UseManagedIdentity"] = useManagedIdentity.ToString(),
            ["Jwt:Key"] = "test-only-signing-key-with-32-characters-minimum",
            ["Jwt:Issuer"] = "https://api.example.invalid",
            ["Jwt:Audience"] = "https://web.example.invalid",
            ["Jwt:ExpiresMinutes"] = "60",
            ["Cors:AllowedOrigins:0"] = "https://web.example.invalid",
            ["MediaStorage:Provider"] = "AzureBlob",
            ["MediaStorage:BlobServiceUri"] = "https://media.blob.core.windows.net",
            ["MediaStorage:ContainerName"] = "media",
            ["MediaStorage:PublicBaseUrl"] = "https://api.example.invalid",
            ["MediaStorage:MaxFileSizeBytes"] = "10485760",
            ["AdminInvitations:SiteUrl"] = "https://web.example.invalid",
            ["AdminInvitations:ExpiresHours"] = "72"
        };
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
