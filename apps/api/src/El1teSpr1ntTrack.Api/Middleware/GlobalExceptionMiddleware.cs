using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Api.Observability;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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

            if (exception is TooManyAttemptsException)
            {
                context.Response.Headers.RetryAfter = "900";
                await WriteProblemAsync(context, StatusCodes.Status429TooManyRequests,
                    "Too many attempts.", "Wait before trying again.");
                return;
            }

            var referenceId = SupportReference.Create();
            var operationId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            logger.LogError(
                "Unhandled API exception. ReferenceId: {ReferenceId}; OperationId: {OperationId}; ExceptionType: {ExceptionType}; StackTrace: {StackTrace}",
                referenceId,
                operationId,
                exception.GetType().FullName,
                exception.StackTrace);
            await WriteUnexpectedProblemAsync(context, referenceId);
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

    private static async Task WriteUnexpectedProblemAsync(HttpContext context, string referenceId)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers[SupportReference.HeaderName] = referenceId;
        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = "The request could not be completed. Contact support if the issue persists.",
            Status = StatusCodes.Status500InternalServerError
        };
        problem.Extensions["referenceId"] = referenceId;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
