using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace El1teSpr1ntTrack.Api.Health;

public static class SafeHealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { status }));
    }
}
