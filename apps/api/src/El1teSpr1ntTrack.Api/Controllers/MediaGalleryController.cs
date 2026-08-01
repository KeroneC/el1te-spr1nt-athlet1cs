using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("media")]
[Tags("Public Media")]
public sealed class MediaController(IMediaService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] int? width, CancellationToken token)
    {
        if (width.HasValue && width is not (480 or 960 or 1600))
            return BadRequest(new ProblemDetails { Title = "Invalid media width", Detail = "Width must be 480, 960, or 1600." });
        var media = await service.OpenPublicAsync(id, width, token);
        if (media is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = media.IsVersionedDerivative
            ? "public,max-age=86400,stale-while-revalidate=604800"
            : "public,max-age=3600";
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
