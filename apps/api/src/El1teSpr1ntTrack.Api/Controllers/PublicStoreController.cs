using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/store")]
[Tags("Public Store")]
public sealed class PublicStoreController(
    IPublicStoreService storeService,
    StoreSettings storeSettings) : ControllerBase
{
    [HttpGet("products")]
    [ProducesResponseType(typeof(PublicStoreCatalogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] PublicStockStatus? availability,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        if (!storeSettings.PublicCatalogEnabled) return NotFound();
        return Ok(await storeService.GetProductsAsync(
            new PublicStoreQueryOptions(search, category, availability, page, pageSize),
            cancellationToken));
    }

    [HttpGet("products/{slug}")]
    [ProducesResponseType(typeof(PublicStoreProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(string slug, CancellationToken cancellationToken)
    {
        if (!storeSettings.PublicCatalogEnabled) return NotFound();
        var product = await storeService.GetProductAsync(slug, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }
}
