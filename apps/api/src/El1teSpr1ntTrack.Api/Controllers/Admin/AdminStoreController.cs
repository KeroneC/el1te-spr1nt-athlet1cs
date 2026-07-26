using System.Security.Claims;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace El1teSpr1ntTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = CmsAdminAuthorization.PolicyName)]
[Route("api/admin/store")]
[Tags("Admin - Store")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminStoreController(IStoreAdminService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken token) =>
        Ok(await service.GetDashboardAsync(token));

    [HttpGet("products")]
    public async Task<IActionResult> Products(
        string? search,
        StoreProductStatus? status,
        Guid? categoryId,
        bool? lowStock,
        int page = 1,
        int pageSize = 20,
        CancellationToken token = default) =>
        Ok(await service.GetProductsAsync(
            new AdminStoreProductOptions(search, status, categoryId, lowStock, page, pageSize),
            token));

    [HttpGet("products/{id:guid}")]
    public async Task<IActionResult> Product(Guid id, CancellationToken token) =>
        Ok(await service.GetProductAsync(id, token));

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(StoreProductWriteDto request, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateProductAsync(request, CurrentUserId(), token));

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, StoreProductWriteDto request, CancellationToken token) =>
        Ok(await service.UpdateProductAsync(id, request, CurrentUserId(), token));

    [HttpPost("products/{id:guid}/duplicate")]
    public async Task<IActionResult> DuplicateProduct(Guid id, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.DuplicateProductAsync(id, CurrentUserId(), token));

    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> ArchiveProduct(Guid id, CancellationToken token)
    {
        await service.ArchiveProductAsync(id, CurrentUserId(), token);
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken token) =>
        Ok(await service.GetCategoriesAsync(token));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(ProductCategoryWriteDto request, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateCategoryAsync(request, CurrentUserId(), token));

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, ProductCategoryWriteDto request, CancellationToken token) =>
        Ok(await service.UpdateCategoryAsync(id, request, CurrentUserId(), token));

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory(
        string? search,
        Guid? productId,
        bool? lowStock,
        bool? isActive,
        int page = 1,
        int pageSize = 50,
        CancellationToken token = default) =>
        Ok(await service.GetInventoryAsync(
            new AdminInventoryOptions(search, productId, lowStock, isActive, page, pageSize),
            token));

    [HttpPost("inventory/{variantId:guid}/adjustments")]
    public async Task<IActionResult> AdjustInventory(
        Guid variantId,
        InventoryAdjustmentWriteDto request,
        CancellationToken token) =>
        Ok(await service.AdjustInventoryAsync(variantId, request, CurrentUserId(), token));

    [HttpPost("inventory/receipts")]
    public async Task<IActionResult> ReceiveInventory(BulkInventoryReceiptDto request, CancellationToken token) =>
        Ok(await service.ReceiveInventoryAsync(request, CurrentUserId(), token));

    [HttpPost("inventory/stocktakes")]
    public async Task<IActionResult> CompleteStocktake(InventoryStocktakeWriteDto request, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.CompleteStocktakeAsync(request, CurrentUserId(), token));

    [HttpGet("inventory/adjustments")]
    public async Task<IActionResult> InventoryHistory(
        Guid? variantId,
        int page = 1,
        int pageSize = 50,
        CancellationToken token = default) =>
        Ok(await service.GetInventoryHistoryAsync(variantId, page, pageSize, token));

    [HttpGet("inventory/stocktakes")]
    public async Task<IActionResult> Stocktakes(
        int page = 1,
        int pageSize = 20,
        CancellationToken token = default) =>
        Ok(await service.GetStocktakesAsync(page, pageSize, token));

    [HttpGet("square-import/preview")]
    [Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
    public async Task<IActionResult> PreviewSquareImport(CancellationToken token) =>
        Ok(await service.PreviewSquareImportAsync(token));

    [HttpPost("square-import")]
    [Authorize(Policy = CmsAdminAuthorization.SuperAdminPolicyName)]
    public async Task<IActionResult> ImportSquareCatalog(CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await service.ImportSquareCatalogAsync(CurrentUserId(), token));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
