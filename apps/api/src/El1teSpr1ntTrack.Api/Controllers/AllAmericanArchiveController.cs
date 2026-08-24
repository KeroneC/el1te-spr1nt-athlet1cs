using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("api/public/all-americans")]
[Tags("Public All-Americans")]
public sealed class AllAmericanArchiveController(IAllAmericanArchiveService service, IConfiguration configuration) : ControllerBase
{
    private bool Enabled => configuration.GetValue<bool>("AllAmericansArchive:Enabled");
    [HttpGet] public async Task<IActionResult> List(int page = 1, int pageSize = 12, CancellationToken token = default) => Enabled ? Ok(await service.GetPublicAsync(page, pageSize, token)) : NotFound();
    [HttpGet("{year}")] public async Task<IActionResult> Get(string year, CancellationToken token)
    { if (!Enabled) return NotFound(); var result = await service.GetPublicAsync(year, token); return result is null ? NotFound() : Ok(result); }
}
