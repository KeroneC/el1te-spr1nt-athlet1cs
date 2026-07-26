using System.Text;
using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/square")]
public sealed class SquareWebhookController(ISquareWebhookService webhookService) : ControllerBase
{
    private const int MaximumWebhookBytes = 256 * 1024;

    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        if (Request.ContentLength > MaximumWebhookBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        if (Encoding.UTF8.GetByteCount(rawBody) > MaximumWebhookBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var result = await webhookService.HandleAsync(
            rawBody,
            Request.Headers["x-square-hmacsha256-signature"].FirstOrDefault(),
            cancellationToken);

        return result switch
        {
            SquareWebhookResult.Accepted => Ok(),
            SquareWebhookResult.Duplicate => Ok(),
            SquareWebhookResult.Disabled => NotFound(),
            SquareWebhookResult.InvalidSignature => StatusCode(StatusCodes.Status403Forbidden),
            SquareWebhookResult.InvalidPayload => BadRequest(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
