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
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=tcp:sql.example.invalid,1433;Initial Catalog=app;Encrypt=True;",
            ["Database:UseManagedIdentity"] = "true",
            ["Jwt:Key"] = "test-only-signing-key-with-32-characters-minimum",
            ["Jwt:Issuer"] = "https://api.example.invalid",
            ["Jwt:Audience"] = "https://web.example.invalid",
            ["Jwt:ExpiresMinutes"] = "60",
            ["Cors:AllowedOrigins:0"] = "https://web.example.invalid",
            ["MediaStorage:Provider"] = "AzureBlob",
            ["MediaStorage:BlobServiceUri"] = "https://media.blob.core.windows.net",
            ["MediaStorage:ContainerName"] = "media",
            ["MediaStorage:PublicBaseUrl"] = "https://api.example.invalid",
            ["MediaStorage:MaxFileSizeBytes"] = "10485760"
        }).Build();

        ProductionConfigurationValidator.Validate(configuration, new TestEnvironment("Production"));
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

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
