using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Detail = "The request could not be completed. Contact support if the issue persists.",
                Status = StatusCodes.Status500InternalServerError
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
