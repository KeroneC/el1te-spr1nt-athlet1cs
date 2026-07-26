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
    Task<AdminStoreProductDto> DuplicateProductAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken);
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
    Task<SquareCatalogImportPreviewDto> PreviewSquareImportAsync(CancellationToken cancellationToken);
    Task<SquareCatalogImportResultDto> ImportSquareCatalogAsync(Guid actorUserId, CancellationToken cancellationToken);
}

public sealed record SquareCatalogSnapshot(IReadOnlyList<SquareCatalogProduct> Products);

public sealed record SquareCatalogProduct(
    string CatalogObjectId,
    long Version,
    string Name,
    string? Description,
    string? CategoryCatalogObjectId,
    string? CategoryName,
    IReadOnlyList<SquareCatalogImage> Images,
    IReadOnlyList<SquareCatalogOption> Options,
    IReadOnlyList<SquareCatalogVariant> Variants);

public sealed record SquareCatalogImage(string CatalogObjectId, string Url, string? Caption);

public sealed record SquareCatalogOption(
    string CatalogObjectId,
    string Name,
    int DisplayOrder,
    IReadOnlyList<SquareCatalogOptionValue> Values);

public sealed record SquareCatalogOptionValue(
    string CatalogObjectId,
    string Name,
    string? ColorHex,
    int DisplayOrder);

public sealed record SquareCatalogVariant(
    string CatalogObjectId,
    long Version,
    string Name,
    string? Sku,
    long PriceMinor,
    string Currency,
    int OnHandQuantity,
    IReadOnlyList<string> OptionValueCatalogObjectIds);

public interface ISquareCatalogImageImporter
{
    Task<Guid?> ImportAsync(
        SquareCatalogImage image,
        string productName,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
