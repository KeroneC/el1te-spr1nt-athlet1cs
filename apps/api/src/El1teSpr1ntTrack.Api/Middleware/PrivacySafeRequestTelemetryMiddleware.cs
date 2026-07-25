using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace El1teSpr1ntTrack.Api.Middleware;

public sealed class PrivacySafeRequestTelemetryMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    private readonly string? releaseSha = NormalizeReleaseSha(configuration["RELEASE_SHA"]);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            var activity = Activity.Current;
            if (activity is not null)
            {
                var routeTemplate = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
                var safeRoute = string.IsNullOrWhiteSpace(routeTemplate)
                    ? "unmatched"
                    : $"/{routeTemplate.TrimStart('/')}";

                activity.DisplayName = $"{context.Request.Method} {safeRoute}";
                activity.SetTag("http.route", safeRoute);
                activity.SetTag("url.query", null);
                activity.SetTag("http.request.header.authorization", null);
                activity.SetTag("http.request.header.cookie", null);
                if (releaseSha is not null)
                {
                    activity.SetTag("ReleaseSha", releaseSha);
                }
            }
        }
    }

    private static string? NormalizeReleaseSha(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed[..Math.Min(trimmed.Length, 40)];
    }
}
