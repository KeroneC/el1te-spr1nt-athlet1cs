using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IStoreAdminService
{
    Task<AdminStoreDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<AdminStoreProductSummaryDto>> GetProductsAsync(AdminStoreProductOptions options, CancellationToken cancellationToken);
    Task<AdminStoreProductDto> GetProductAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminStoreProductDto> CreateProductAsync(StoreProductWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminStoreProductDto> UpdateProductAsync(Guid id, StoreProductWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminStoreProductDto> DuplicateProductAsync(Guid id, DuplicateProductWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminStoreProductDto> RegenerateProductSlugAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken);
    Task ArchiveProductAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<AdminProductCategoryDto> CreateCategoryAsync(ProductCategoryWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminProductCategoryDto> UpdateCategoryAsync(Guid id, ProductCategoryWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminInventoryVariantDto>> GetInventoryAsync(AdminInventoryOptions options, CancellationToken cancellationToken);
    Task<AdminInventoryVariantDto> AdjustInventoryAsync(Guid variantId, InventoryAdjustmentWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminInventoryVariantDto>> ReceiveInventoryAsync(BulkInventoryReceiptDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminInventoryStocktakeDto> CompleteStocktakeAsync(InventoryStocktakeWriteDto request, Guid actorUserId, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminInventoryAdjustmentDto>> GetInventoryHistoryAsync(Guid? variantId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResultDto<AdminInventoryStocktakeDto>> GetStocktakesAsync(int page, int pageSize, CancellationToken cancellationToken);
}
