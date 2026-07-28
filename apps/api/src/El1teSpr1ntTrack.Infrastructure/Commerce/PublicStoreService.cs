using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class PublicStoreService(El1teDbContext db) : IPublicStoreService
{
    public async Task<PublicStoreCatalogDto> GetProductsAsync(
        PublicStoreQueryOptions options,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, options.Page);
        var pageSize = Math.Clamp(options.PageSize, 1, 48);
        var query = db.Products
            .AsNoTracking()
            .Where(product => product.Status == StoreProductStatus.Published)
            .Include(product => product.Category)
            .Include(product => product.Media)
                .ThenInclude(media => media.MediaAsset)
            .Include(product => product.Variants)
            .AsSplitQuery()
            .AsQueryable();

        var search = options.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product =>
                EF.Functions.Like(product.Name, $"%{search}%") ||
                (product.ShortDescription != null && EF.Functions.Like(product.ShortDescription, $"%{search}%")));
        }

        var category = options.Category?.Trim();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(product => product.Category != null && product.Category.Slug == category);
        }

        var products = await query
            .OrderByDescending(product => product.IsFeatured)
            .ThenBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToListAsync(cancellationToken);

        var summaries = products.Select(ToSummary).ToList();
        if (options.Availability is not null)
        {
            summaries = summaries
                .Where(product => product.Availability == options.Availability)
                .ToList();
        }

        var categories = await db.ProductCategories
            .AsNoTracking()
            .Where(value => value.IsActive &&
                value.Products.Any(product => product.Status == StoreProductStatus.Published))
            .OrderBy(value => value.DisplayOrder)
            .ThenBy(value => value.Name)
            .Select(value => new PublicStoreCategoryDto(
                value.Name,
                value.Slug,
                value.Products.Count(product => product.Status == StoreProductStatus.Published)))
            .ToListAsync(cancellationToken);

        var totalCount = summaries.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PublicStoreCatalogDto(
            summaries.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            categories,
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<PublicStoreProductDto?> GetProductAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(value => value.Status == StoreProductStatus.Published && value.Slug == slug)
            .Include(value => value.Category)
            .Include(value => value.Media)
                .ThenInclude(value => value.MediaAsset)
            .Include(value => value.Options)
                .ThenInclude(value => value.Values)
                    .ThenInclude(value => value.SwatchMediaAsset)
            .Include(value => value.Variants)
                .ThenInclude(value => value.OptionValues)
            .Include(value => value.ModifierGroups)
                .ThenInclude(value => value.Values)
                    .ThenInclude(value => value.OverlayMediaAsset)
            .Include(value => value.VisualizerLayers)
                .ThenInclude(value => value.MediaAsset)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);

        return product is null ? null : ToDetail(product);
    }

    internal static PublicStockStatus StockStatus(ProductVariant variant)
    {
        var available = Math.Max(0, variant.OnHandQuantity - variant.ReservedQuantity);
        if (available == 0) return PublicStockStatus.SoldOut;
        return available <= variant.LowStockThreshold
            ? PublicStockStatus.LowStock
            : PublicStockStatus.InStock;
    }

    private static PublicStockStatus ProductStockStatus(IEnumerable<ProductVariant> variants)
    {
        var statuses = variants.Where(value => value.IsActive).Select(StockStatus).ToList();
        if (statuses.Count == 0 || statuses.All(value => value == PublicStockStatus.SoldOut))
            return PublicStockStatus.SoldOut;
        if (statuses.Any(value => value == PublicStockStatus.InStock))
            return PublicStockStatus.InStock;
        return PublicStockStatus.LowStock;
    }

    private static PublicStoreProductSummaryDto ToSummary(Product product)
    {
        var variants = product.Variants.Where(value => value.IsActive).ToList();
        var prices = variants.Select(value => value.PriceOverrideMinor ?? product.BasePriceMinor).ToList();
        if (prices.Count == 0) prices.Add(product.BasePriceMinor);
        var image = product.Media
            .Where(value => value.MediaAsset.IsActive)
            .OrderBy(value => value.DisplayOrder)
            .FirstOrDefault();
        return new PublicStoreProductSummaryDto(
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.Category?.Name,
            product.Category?.Slug,
            prices.Min(),
            prices.Max(),
            product.Currency,
            product.IsFeatured,
            image?.MediaAsset.PublicUrl,
            image is null ? null : image.AltTextOverride ?? image.MediaAsset.AltText,
            ProductStockStatus(variants));
    }

    private static PublicStoreProductDto ToDetail(Product product)
    {
        var variants = product.Variants
            .Where(value => value.IsActive)
            .OrderBy(value => value.Name)
            .Select(value => new PublicProductVariantDto(
                value.Id,
                value.Name,
                value.PriceOverrideMinor ?? product.BasePriceMinor,
                StockStatus(value),
                value.OptionValues.Select(option => option.ProductOptionValueId).ToList()))
            .ToList();

        var media = product.Media
            .Where(value => value.MediaAsset.IsActive)
            .OrderBy(value => value.DisplayOrder)
            .Select(value => new PublicProductMediaDto(
                value.MediaAssetId,
                value.MediaAsset.PublicUrl,
                value.AltTextOverride ?? value.MediaAsset.AltText,
                value.Role,
                value.DisplayOrder))
            .ToList();

        var options = product.Options
            .Where(value => value.IsActive && value.IsTracked)
            .OrderBy(value => value.DisplayOrder)
            .Select(value => new PublicProductOptionDto(
                value.Id,
                value.Name,
                value.DisplayOrder,
                value.Values
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.DisplayOrder)
                    .Select(option => new PublicProductOptionValueDto(
                        option.Id,
                        option.Name,
                        option.Slug,
                        option.ColorHex,
                        option.SwatchMediaAsset is { IsActive: true } ? option.SwatchMediaAsset.PublicUrl : null,
                        option.DisplayOrder))
                    .ToList()))
            .ToList();

        var modifiers = product.ModifierGroups
            .Where(value => value.IsActive)
            .OrderBy(value => value.DisplayOrder)
            .Select(value => new PublicProductModifierGroupDto(
                value.Id,
                value.Name,
                value.Type,
                value.IsRequired,
                value.MinimumSelections,
                value.MaximumSelections,
                value.DisplayOrder,
                value.Values
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.DisplayOrder)
                    .Select(option => new PublicProductModifierValueDto(
                        option.Id,
                        option.Name,
                        option.PriceAdjustmentMinor,
                        option.ColorHex,
                        option.OverlayMediaAsset is { IsActive: true } ? option.OverlayMediaAsset.PublicUrl : null,
                        option.DisplayOrder))
                    .ToList()))
            .ToList();

        var layers = product.VisualizerLayers
            .Where(value => value.MediaAsset.IsActive)
            .OrderBy(value => value.ZIndex)
            .Select(value => new PublicProductVisualizerLayerDto(
                value.MediaAssetId,
                value.MediaAsset.PublicUrl,
                value.ProductOptionValueId,
                value.ProductModifierValueId,
                value.XPercent,
                value.YPercent,
                value.WidthPercent,
                value.HeightPercent,
                value.ZIndex,
                SafeBlendMode(value.BlendMode)))
            .ToList();

        return new PublicStoreProductDto(
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.Description,
            product.Category?.Name,
            product.BasePriceMinor,
            product.Currency,
            product.AllowsSpecialRequests,
            ProductStockStatus(product.Variants),
            media,
            options,
            variants,
            modifiers,
            layers);
    }

    private static string SafeBlendMode(string value) =>
        value is "normal" or "multiply" or "screen" or "overlay" ? value : "normal";
}
