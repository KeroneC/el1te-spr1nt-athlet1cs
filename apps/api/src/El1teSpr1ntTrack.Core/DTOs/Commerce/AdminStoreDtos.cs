using System.ComponentModel.DataAnnotations;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Commerce;

public sealed record AdminStoreDashboardDto(
    int DraftProducts,
    int PublishedProducts,
    int ActiveVariants,
    int LowStockVariants,
    int SoldOutVariants,
    int TotalOnHand);

public sealed record AdminProductCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsActive,
    int ProductCount,
    string? SquareCatalogObjectId);

public sealed class ProductCategoryWriteDto
{
    [Required, MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record AdminStoreProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? CategoryName,
    long BasePriceMinor,
    string Currency,
    StoreProductStatus Status,
    bool IsFeatured,
    int DisplayOrder,
    ProductFulfillmentMode FulfillmentMode,
    int VariantCount,
    int TotalOnHand,
    int TotalAvailable,
    int LowStockVariantCount,
    string? PrimaryImageUrl,
    string? SquareCatalogObjectId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AdminStoreProductDto(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    long BasePriceMinor,
    string Currency,
    StoreProductStatus Status,
    bool IsFeatured,
    int DisplayOrder,
    bool AllowsSpecialRequests,
    ProductFulfillmentMode FulfillmentMode,
    string? PrintifyProductId,
    int? PrintifyBlueprintId,
    int? PrintifyProviderId,
    DateTimeOffset? PrintifyLastSyncedAtUtc,
    string? SquareCatalogObjectId,
    long? SquareCatalogVersion,
    DateTimeOffset? ImportedAtUtc,
    IReadOnlyList<AdminProductMediaDto> Media,
    IReadOnlyList<AdminProductOptionDto> Options,
    IReadOnlyList<AdminProductVariantDto> Variants,
    IReadOnlyList<AdminProductModifierGroupDto> ModifierGroups,
    IReadOnlyList<AdminProductVisualizerLayerDto> VisualizerLayers,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AdminProductMediaDto(
    Guid Id,
    Guid MediaAssetId,
    string PublicUrl,
    string Title,
    string AltText,
    ProductMediaRole Role,
    string? AltTextOverride,
    int DisplayOrder);

public sealed record AdminProductOptionDto(
    Guid Id,
    string Name,
    bool IsTracked,
    int DisplayOrder,
    bool IsActive,
    string? SquareCatalogObjectId,
    IReadOnlyList<AdminProductOptionValueDto> Values);

public sealed record AdminProductOptionValueDto(
    Guid Id,
    string Name,
    string Slug,
    string? ColorHex,
    Guid? SwatchMediaAssetId,
    int DisplayOrder,
    bool IsActive,
    string? SquareCatalogObjectId);

public sealed record AdminProductVariantDto(
    Guid Id,
    string Name,
    string Sku,
    long? PriceOverrideMinor,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int LowStockThreshold,
    bool IsActive,
    string? SquareCatalogObjectId,
    long? SquareCatalogVersion,
    int? PrintifyVariantId,
    long? PrintifyProviderCostMinor,
    bool PrintifyIsAvailable,
    DateTimeOffset? PrintifyLastVerifiedAtUtc,
    string RowVersion,
    IReadOnlyList<Guid> OptionValueIds);

public sealed record AdminProductModifierGroupDto(
    Guid Id,
    string Name,
    ProductModifierType Type,
    bool IsRequired,
    int MinimumSelections,
    int MaximumSelections,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AdminProductModifierValueDto> Values);

public sealed record AdminProductModifierValueDto(
    Guid Id,
    string Name,
    long PriceAdjustmentMinor,
    string? ColorHex,
    Guid? OverlayMediaAssetId,
    int DisplayOrder,
    bool IsActive);

public sealed record AdminProductVisualizerLayerDto(
    Guid Id,
    Guid MediaAssetId,
    Guid? ProductOptionValueId,
    Guid? ProductModifierValueId,
    decimal XPercent,
    decimal YPercent,
    decimal WidthPercent,
    decimal HeightPercent,
    int ZIndex,
    string BlendMode);

public sealed class StoreProductWriteDto
{
    public Guid? CategoryId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; init; }

    [MaxLength(5000)]
    public string? Description { get; init; }

    [Range(0, long.MaxValue)]
    public long BasePriceMinor { get; init; }

    public StoreProductStatus Status { get; init; } = StoreProductStatus.Draft;
    public bool IsFeatured { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }

    public bool AllowsSpecialRequests { get; init; }
    public ProductFulfillmentMode FulfillmentMode { get; init; } = ProductFulfillmentMode.ClubHandoff;
    public string? PrintifyProductId { get; init; }
    public int? PrintifyBlueprintId { get; init; }
    public int? PrintifyProviderId { get; init; }
    public IReadOnlyList<ProductMediaWriteDto> Media { get; init; } = [];
    public IReadOnlyList<ProductOptionWriteDto> Options { get; init; } = [];
    public IReadOnlyList<ProductVariantWriteDto> Variants { get; init; } = [];
    public IReadOnlyList<ProductModifierGroupWriteDto> ModifierGroups { get; init; } = [];
    public IReadOnlyList<ProductVisualizerLayerWriteDto> VisualizerLayers { get; init; } = [];
}

public sealed record ProductMediaWriteDto(
    Guid Id,
    Guid MediaAssetId,
    ProductMediaRole Role,
    string? AltTextOverride,
    int DisplayOrder);

public sealed record ProductOptionWriteDto(
    Guid Id,
    string Name,
    bool IsTracked,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<ProductOptionValueWriteDto> Values);

public sealed record ProductOptionValueWriteDto(
    Guid Id,
    string Name,
    string? ColorHex,
    Guid? SwatchMediaAssetId,
    int DisplayOrder,
    bool IsActive);

public sealed record ProductVariantWriteDto(
    Guid Id,
    string Name,
    string Sku,
    long? PriceOverrideMinor,
    int LowStockThreshold,
    bool IsActive,
    string? RowVersion,
    int? PrintifyVariantId,
    IReadOnlyList<Guid> OptionValueIds);

public sealed record ProductModifierGroupWriteDto(
    Guid Id,
    string Name,
    ProductModifierType Type,
    bool IsRequired,
    int MinimumSelections,
    int MaximumSelections,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<ProductModifierValueWriteDto> Values);

public sealed record ProductModifierValueWriteDto(
    Guid Id,
    string Name,
    long PriceAdjustmentMinor,
    string? ColorHex,
    Guid? OverlayMediaAssetId,
    int DisplayOrder,
    bool IsActive);

public sealed record ProductVisualizerLayerWriteDto(
    Guid Id,
    Guid MediaAssetId,
    Guid? ProductOptionValueId,
    Guid? ProductModifierValueId,
    decimal XPercent,
    decimal YPercent,
    decimal WidthPercent,
    decimal HeightPercent,
    int ZIndex,
    string BlendMode);

public sealed record AdminInventoryVariantDto(
    Guid ProductId,
    string ProductName,
    Guid VariantId,
    string VariantName,
    string Sku,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int LowStockThreshold,
    bool IsLowStock,
    bool IsSoldOut,
    bool IsActive,
    string RowVersion,
    DateTimeOffset? UpdatedAtUtc);

public sealed class InventoryAdjustmentWriteDto
{
    [Range(-100000, 100000)]
    public int QuantityDelta { get; init; }

    public InventoryAdjustmentReason Reason { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed record BulkInventoryReceiptLineDto(Guid VariantId, int Quantity, string RowVersion);

public sealed class BulkInventoryReceiptDto
{
    public IReadOnlyList<BulkInventoryReceiptLineDto> Lines { get; init; } = [];

    [MaxLength(1000)]
    public string? Note { get; init; }
}

public sealed record InventoryStocktakeLineWriteDto(Guid VariantId, int CountedOnHandQuantity, string RowVersion);

public sealed class InventoryStocktakeWriteDto
{
    public IReadOnlyList<InventoryStocktakeLineWriteDto> Lines { get; init; } = [];

    [MaxLength(1000)]
    public string? Note { get; init; }
}

public sealed record AdminInventoryAdjustmentDto(
    Guid Id,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    InventoryAdjustmentReason Reason,
    int QuantityDelta,
    int ResultingOnHandQuantity,
    string? Note,
    string ActorDisplayName,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminInventoryStocktakeDto(
    Guid Id,
    int VariantCount,
    int ChangedVariantCount,
    string? Note,
    string ActorDisplayName,
    DateTimeOffset CreatedAtUtc);

public sealed record SquareCatalogImportProductPreviewDto(
    string SquareCatalogObjectId,
    string Name,
    int VariantCount,
    int ImageCount,
    bool AlreadyImported);

public sealed record SquareCatalogImportPreviewDto(
    bool IsConfigured,
    int ProductCount,
    int NewProductCount,
    IReadOnlyList<SquareCatalogImportProductPreviewDto> Products);

public sealed record SquareCatalogImportResultDto(
    Guid ImportRunId,
    int ProductsDiscovered,
    int ProductsCreated,
    int ProductsSkipped,
    int ImagesImported);

public sealed record PrintifyIntegrationHealthDto(
    bool Enabled,
    bool IsConfigured,
    bool ConnectionHealthy,
    string? ShopTitle,
    long? ShopId,
    DateTimeOffset? TokenExpiresAtUtc,
    bool TokenExpiresWithinThirtyDays,
    bool WebhookConfigured,
    int ExpectedWebhookCount,
    int ActiveWebhookCount,
    int MappedProductCount,
    int MappingIssueCount,
    int CostChangeCount,
    DateTimeOffset? LastCatalogSyncAtUtc,
    long MinimumGrossContributionMinor,
    bool OrderCreationEnabled,
    bool ProductionReleaseEnabled);

public sealed record PrintifyCatalogPreviewProductDto(
    string PrintifyProductId,
    string Name,
    int VariantCount,
    int AvailableVariantCount,
    int ImageCount,
    bool AlreadyConnected);

public sealed record PrintifyCatalogPreviewDto(
    bool IsConfigured,
    string? ShopTitle,
    int ProductCount,
    int NewProductCount,
    IReadOnlyList<PrintifyCatalogPreviewProductDto> Products);

public sealed class PrintifyCatalogImportRequestDto
{
    [MinLength(1), MaxLength(100)]
    public IReadOnlyList<string> ProductIds { get; init; } = [];
}

public sealed record PrintifyCatalogImportResultDto(
    Guid ImportRunId,
    int ProductsDiscovered,
    int ProductsCreated,
    int ProductsSkipped,
    int ImagesImported);

public sealed record PrintifyRefreshResultDto(
    int ProductsChecked,
    int VariantsChecked,
    int AvailabilityChanges,
    int CostChanges,
    int MappingIssues,
    DateTimeOffset CompletedAtUtc);
