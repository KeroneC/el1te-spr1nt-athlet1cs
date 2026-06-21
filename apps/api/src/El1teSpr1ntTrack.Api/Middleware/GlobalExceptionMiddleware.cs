using El1teSpr1ntTrack.Application.Common.Exceptions;
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
            if (exception is CmsRequestValidationException validationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new HttpValidationProblemDetails(validationException.Errors)
                {
                    Title = "The request is invalid.",
                    Status = StatusCodes.Status400BadRequest
                });
                return;
            }

            if (exception is CmsNotFoundException)
            {
                await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Resource not found.", exception.Message);
                return;
            }

            if (exception is CmsConflictException)
            {
                await WriteProblemAsync(context, StatusCodes.Status409Conflict, "The request conflicts with existing content.", exception.Message);
                return;
            }

            logger.LogError(exception, "Unhandled API exception.");
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "The request could not be completed. Contact support if the issue persists.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        });
    }
}
