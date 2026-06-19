using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "El1te Spr1nt Athlet1cs API",
            checkedAt = DateTimeOffset.UtcNow
        });
    }
}
