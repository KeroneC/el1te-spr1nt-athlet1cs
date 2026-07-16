using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("media")]
[Tags("Public Media")]
public sealed class MediaController(IMediaService service) : ControllerBase
{
    private static readonly HashSet<int> AllowedWidths = [480, 800, 1200, 1600];

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(Guid id, int? width, CancellationToken token)
    {
        if (width is not null && !AllowedWidths.Contains(width.Value))
            return BadRequest(new { message = "Choose a supported image width: 480, 800, 1200, or 1600 pixels." });
        var media = await service.OpenPublicAsync(id, width, token);
        if (media is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "public,max-age=3600,stale-while-revalidate=86400";
        return File(media.Stream, media.ContentType, enableRangeProcessing: true);
    }
}

[ApiController]
[Route("api/public/gallery-albums")]
[Tags("Public Gallery")]
public sealed class PublicGalleryController(IGalleryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 12, CancellationToken token = default) => Ok(await service.GetPublicAsync(page, pageSize, token));

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken token)
    {
        var album = await service.GetPublicAsync(slug, token);
        return album is null ? NotFound() : Ok(album);
    }
}
