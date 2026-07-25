using System.Text.Json;
using System.Diagnostics;
using El1teSpr1ntTrack.Api.Middleware;
using El1teSpr1ntTrack.Api.Observability;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class ObservabilityTests
{
    [Fact]
    public async Task UnexpectedExceptionReturnsMatchingSafeReference()
    {
        var context = Context();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("private diagnostic value"),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var bodyReference = document.RootElement.GetProperty("referenceId").GetString();
        var headerReference = context.Response.Headers[SupportReference.HeaderName].ToString();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(headerReference, bodyReference);
        Assert.True(SupportReference.IsValid(bodyReference));
        Assert.DoesNotContain("private diagnostic value", document.RootElement.ToString());
    }

    [Fact]
    public async Task ExpectedValidationFailureDoesNotReturnReference()
    {
        var context = Context();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new CmsRequestValidationException(new Dictionary<string, string[]>
            {
                ["Title"] = ["Title is required."]
            }),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(SupportReference.HeaderName));
        Assert.False(document.RootElement.TryGetProperty("referenceId", out _));
    }

    [Fact]
    public async Task TelemetryUsesRouteTemplateAndRemovesSensitiveRequestTags()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.test");
        context.Request.Method = "PUT";
        context.Request.QueryString = new QueryString("?email=private@example.test");
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/admin/users/{id:guid}"),
            0,
            EndpointMetadataCollection.Empty,
            "user update"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["RELEASE_SHA"] = new string('a', 40) })
            .Build();
        using var activity = new Activity("raw request");
        activity.SetTag("url.query", "email=private@example.test");
        activity.SetTag("http.request.header.authorization", "Bearer private-token");
        activity.Start();
        var middleware = new PrivacySafeRequestTelemetryMiddleware(_ => Task.CompletedTask, configuration);

        await middleware.InvokeAsync(context);

        Assert.Equal("PUT /api/admin/users/{id:guid}", activity.DisplayName);
        Assert.Equal("/api/admin/users/{id:guid}", activity.GetTagItem("http.route"));
        Assert.Null(activity.GetTagItem("url.query"));
        Assert.Null(activity.GetTagItem("http.request.header.authorization"));
        Assert.Equal(new string('a', 40), activity.GetTagItem("ReleaseSha"));
    }

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
