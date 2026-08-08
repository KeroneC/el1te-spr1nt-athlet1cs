using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/printify")]
[Tags("Webhooks - Printify")]
public sealed class PrintifyWebhookController(IPrintifyWebhookService webhookService) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var result = await webhookService.HandleAsync(
            rawBody,
            Request.Headers["X-Pfy-Signature"].FirstOrDefault(),
            cancellationToken);
        return result switch
        {
            PrintifyWebhookResult.Accepted => Ok(),
            PrintifyWebhookResult.Duplicate => Ok(),
            PrintifyWebhookResult.Disabled => NotFound(),
            PrintifyWebhookResult.InvalidSignature => StatusCode(StatusCodes.Status403Forbidden),
            PrintifyWebhookResult.InvalidPayload => BadRequest(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
