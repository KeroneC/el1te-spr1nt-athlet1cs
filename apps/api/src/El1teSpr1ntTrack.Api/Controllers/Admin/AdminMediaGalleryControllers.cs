using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

public sealed class MediaUploadRequest
{
    public required IFormFile File { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public string? Caption { get; init; }
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/media")]
[Tags("Admin - Media")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminMediaController(IMediaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, bool? isActive, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetAsync(new AdminMediaOptions(search, isActive, page, pageSize), token));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetAsync(id, token));

    [HttpPost, Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] MediaUploadRequest request, CancellationToken token)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = request.File.OpenReadStream();
        var asset = await service.UploadAsync(stream, request.File.Length, request.File.FileName,
            request.File.ContentType, request.Title, request.AltText, request.Caption, userId, token);
        return StatusCode(StatusCodes.Status201Created, asset);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MediaMetadataUpdateDto request, CancellationToken token) =>
        Ok(await service.UpdateAsync(id, request, token));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return NoContent();
    }
}

[ApiController, Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/gallery-albums")]
[Tags("Admin - Gallery")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminGalleryAlbumsController(IGalleryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, bool? isPublished, int page = 1, int pageSize = 20, CancellationToken token = default) =>
        Ok(await service.GetAdminAsync(new AdminGalleryOptions(search, isPublished, page, pageSize), token));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetAdminAsync(id, token));
    [HttpPost]
    public async Task<IActionResult> Create(GalleryAlbumWriteDto request, CancellationToken token) => StatusCode(201, await service.CreateAsync(request, token));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, GalleryAlbumWriteDto request, CancellationToken token) => Ok(await service.UpdateAsync(id, request, token));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }
    [HttpPost("{id:guid}/media")]
    public async Task<IActionResult> AddMedia(Guid id, GalleryAlbumMediaWriteDto request, CancellationToken token) => StatusCode(201, await service.AddMediaAsync(id, request, token));
    [HttpPut("{id:guid}/media/{albumMediaId:guid}")]
    public async Task<IActionResult> UpdateMedia(Guid id, Guid albumMediaId, GalleryAlbumMediaWriteDto request, CancellationToken token) => Ok(await service.UpdateMediaAsync(id, albumMediaId, request, token));
    [HttpDelete("{id:guid}/media/{albumMediaId:guid}")]
    public async Task<IActionResult> RemoveMedia(Guid id, Guid albumMediaId, CancellationToken token) { await service.RemoveMediaAsync(id, albumMediaId, token); return NoContent(); }
    [HttpPut("{id:guid}/media/order")]
    public async Task<IActionResult> Reorder(Guid id, GalleryMediaOrderDto request, CancellationToken token) => Ok(await service.ReorderAsync(id, request, token));
}
