using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.DTOs.Commerce;

public sealed record PublicStoreCatalogDto(
    IReadOnlyList<PublicStoreProductSummaryDto> Items,
    IReadOnlyList<PublicStoreCategoryDto> Categories,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PublicStoreCategoryDto(
    string Name,
    string Slug,
    int ProductCount);

public sealed record PublicStoreProductSummaryDto(
    string Name,
    string Slug,
    string? ShortDescription,
    string? CategoryName,
    string? CategorySlug,
    long MinimumPriceMinor,
    long MaximumPriceMinor,
    string Currency,
    bool IsFeatured,
    ProductFulfillmentMode FulfillmentMode,
    string? PrimaryImageUrl,
    string? PrimaryImageAltText,
    PublicStockStatus Availability);

public sealed record PublicStoreProductDto(
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? CategoryName,
    long BasePriceMinor,
    string Currency,
    bool AllowsSpecialRequests,
    ProductFulfillmentMode FulfillmentMode,
    PublicStockStatus Availability,
    IReadOnlyList<PublicProductMediaDto> Media,
    IReadOnlyList<PublicProductOptionDto> Options,
    IReadOnlyList<PublicProductVariantDto> Variants,
    IReadOnlyList<PublicProductModifierGroupDto> ModifierGroups,
    IReadOnlyList<PublicProductVisualizerLayerDto> VisualizerLayers);

public sealed record PublicProductMediaDto(
    Guid MediaAssetId,
    string PublicUrl,
    string AltText,
    ProductMediaRole Role,
    int DisplayOrder);

public sealed record PublicProductOptionDto(
    Guid Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<PublicProductOptionValueDto> Values);

public sealed record PublicProductOptionValueDto(
    Guid Id,
    string Name,
    string Slug,
    string? ColorHex,
    string? SwatchImageUrl,
    int DisplayOrder);

public sealed record PublicProductVariantDto(
    Guid Id,
    string Name,
    long PriceMinor,
    PublicStockStatus Availability,
    IReadOnlyList<Guid> OptionValueIds);

public sealed record PublicProductModifierGroupDto(
    Guid Id,
    string Name,
    ProductModifierType Type,
    bool IsRequired,
    int MinimumSelections,
    int MaximumSelections,
    int DisplayOrder,
    IReadOnlyList<PublicProductModifierValueDto> Values);

public sealed record PublicProductModifierValueDto(
    Guid Id,
    string Name,
    long PriceAdjustmentMinor,
    string? ColorHex,
    string? OverlayImageUrl,
    int DisplayOrder);

public sealed record PublicProductVisualizerLayerDto(
    Guid MediaAssetId,
    string PublicUrl,
    Guid? ProductOptionValueId,
    Guid? ProductModifierValueId,
    decimal XPercent,
    decimal YPercent,
    decimal WidthPercent,
    decimal HeightPercent,
    int ZIndex,
    string BlendMode);

public sealed record PublicStoreQueryOptions(
    string? Search,
    string? Category,
    PublicStockStatus? Availability,
    int Page = 1,
    int PageSize = 12);
