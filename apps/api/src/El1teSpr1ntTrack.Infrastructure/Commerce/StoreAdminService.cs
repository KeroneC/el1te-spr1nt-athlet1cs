using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class StoreAdminService(
    El1teDbContext dbContext,
    ISlugGenerator slugGenerator,
    IClock clock) : IStoreAdminService
{
    public async Task<AdminStoreDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products.AsNoTracking()
            .GroupBy(value => value.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(value => value.Status, value => value.Count, cancellationToken);
        var variants = await dbContext.ProductVariants.AsNoTracking()
            .Where(value => value.IsActive && value.Product.Status != StoreProductStatus.Archived)
            .Select(value => new
            {
                value.OnHandQuantity,
                Available = value.OnHandQuantity - value.ReservedQuantity,
                value.LowStockThreshold
            })
            .ToListAsync(cancellationToken);

        return new AdminStoreDashboardDto(
            products.GetValueOrDefault(StoreProductStatus.Draft),
            products.GetValueOrDefault(StoreProductStatus.Published),
            variants.Count,
            variants.Count(value => value.Available > 0 && value.Available <= value.LowStockThreshold),
            variants.Count(value => value.Available <= 0),
            variants.Sum(value => value.OnHandQuantity));
    }

    public async Task<PagedResultDto<AdminStoreProductSummaryDto>> GetProductsAsync(
        AdminStoreProductOptions options,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = NormalizePage(options.Page, options.PageSize, 100);
        var query = dbContext.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(value =>
                value.Name.Contains(search) ||
                value.Slug.Contains(search) ||
                value.Variants.Any(variant => variant.Sku.Contains(search)));
        }
        if (options.Status.HasValue) query = query.Where(value => value.Status == options.Status);
        if (options.CategoryId.HasValue) query = query.Where(value => value.CategoryId == options.CategoryId);
        if (options.LowStock.HasValue)
        {
            query = options.LowStock.Value
                ? query.Where(value => value.Variants.Any(variant =>
                    variant.IsActive &&
                    variant.OnHandQuantity - variant.ReservedQuantity <= variant.LowStockThreshold))
                : query.Where(value => !value.Variants.Any(variant =>
                    variant.IsActive &&
                    variant.OnHandQuantity - variant.ReservedQuantity <= variant.LowStockThreshold));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(value => value.Status)
            .ThenBy(value => value.DisplayOrder)
            .ThenBy(value => value.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(value => new AdminStoreProductSummaryDto(
                value.Id,
                value.Name,
                value.Slug,
                value.Category == null ? null : value.Category.Name,
                value.BasePriceMinor,
                value.Currency,
                value.Status,
                value.IsFeatured,
                value.DisplayOrder,
                value.Variants.Count(variant => variant.IsActive),
                value.Variants.Sum(variant => variant.IsActive ? variant.OnHandQuantity : 0),
                value.Variants.Sum(variant =>
                    variant.IsActive ? variant.OnHandQuantity - variant.ReservedQuantity : 0),
                value.Variants.Count(variant =>
                    variant.IsActive &&
                    variant.OnHandQuantity - variant.ReservedQuantity > 0 &&
                    variant.OnHandQuantity - variant.ReservedQuantity <= variant.LowStockThreshold),
                value.Media.OrderBy(media => media.Role).ThenBy(media => media.DisplayOrder)
                    .Select(media => media.MediaAsset.PublicUrl).FirstOrDefault(),
                value.SquareCatalogObjectId,
                value.CreatedAt,
                value.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AdminStoreProductSummaryDto>(items, page, pageSize, total);
    }

    public async Task<AdminStoreProductDto> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        MapProduct(await ProductGraph(id, cancellationToken)
            ?? throw new CmsNotFoundException("Product", id));

    public async Task<AdminStoreProductDto> CreateProductAsync(
        StoreProductWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await ValidateProductWrite(request, null, cancellationToken);
        var now = clock.UtcNow;
        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Slug = await UniqueProductSlug(request.Name, null, cancellationToken),
            ShortDescription = Clean(request.ShortDescription),
            Description = Clean(request.Description),
            BasePriceMinor = request.BasePriceMinor,
            Currency = "USD",
            Status = request.Status,
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder,
            AllowsSpecialRequests = request.AllowsSpecialRequests,
            CreatedAt = now
        };
        dbContext.Products.Add(product);
        ApplyNewGraph(product, request, now);
        AddActivity(actorUserId, "store.product.created", "Product", product.Id, $"Created product '{product.Name}'.");
        await SaveAsync(cancellationToken);
        return MapProduct((await ProductGraph(product.Id, cancellationToken))!);
    }

    public async Task<AdminStoreProductDto> UpdateProductAsync(
        Guid id,
        StoreProductWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var product = await ProductGraph(id, cancellationToken)
            ?? throw new CmsNotFoundException("Product", id);
        await ValidateProductWrite(request, id, cancellationToken);
        var now = clock.UtcNow;
        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.ShortDescription = Clean(request.ShortDescription);
        product.Description = Clean(request.Description);
        product.BasePriceMinor = request.BasePriceMinor;
        product.Status = request.Status;
        product.IsFeatured = request.IsFeatured;
        product.DisplayOrder = request.DisplayOrder;
        product.AllowsSpecialRequests = request.AllowsSpecialRequests;
        product.UpdatedAt = now;

        SyncMedia(product, request.Media, now);
        SyncOptions(product, request.Options, now);
        SyncVariants(product, request.Variants, now);
        SyncModifiers(product, request.ModifierGroups, now);
        SyncVisualizerLayers(product, request.VisualizerLayers, now);
        AddActivity(actorUserId, "store.product.updated", "Product", product.Id, $"Updated product '{product.Name}'.");
        await SaveAsync(cancellationToken);
        return MapProduct((await ProductGraph(product.Id, cancellationToken))!);
    }

    public async Task<AdminStoreProductDto> DuplicateProductAsync(
        Guid id,
        DuplicateProductWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var source = await ProductGraph(id, cancellationToken)
            ?? throw new CmsNotFoundException("Product", id);
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            Invalid("Name", "A new product name is required.");
        if (name.Length > 200)
            Invalid("Name", "The product name cannot exceed 200 characters.");
        if (string.Equals(name, source.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            Invalid("Name", "Enter a name that is different from the product being copied.");
        var now = clock.UtcNow;
        var product = new Product
        {
            CategoryId = source.CategoryId,
            Name = name,
            Slug = await UniqueProductSlug(name, null, cancellationToken),
            ShortDescription = source.ShortDescription,
            Description = source.Description,
            BasePriceMinor = source.BasePriceMinor,
            Currency = "USD",
            Status = StoreProductStatus.Draft,
            IsFeatured = false,
            DisplayOrder = source.DisplayOrder,
            AllowsSpecialRequests = source.AllowsSpecialRequests,
            CreatedAt = now
        };
        var valueMap = new Dictionary<Guid, Guid>();
        foreach (var option in source.Options.OrderBy(value => value.DisplayOrder))
        {
            var clone = new ProductOption
            {
                Id = Guid.NewGuid(), Product = product, Name = option.Name, IsTracked = option.IsTracked,
                DisplayOrder = option.DisplayOrder, IsActive = option.IsActive, CreatedAt = now
            };
            foreach (var value in option.Values.OrderBy(value => value.DisplayOrder))
            {
                var cloneValue = new ProductOptionValue
                {
                    Id = Guid.NewGuid(), ProductOption = clone, Name = value.Name, Slug = value.Slug,
                    ColorHex = value.ColorHex, SwatchMediaAssetId = value.SwatchMediaAssetId,
                    DisplayOrder = value.DisplayOrder, IsActive = value.IsActive, CreatedAt = now
                };
                valueMap[value.Id] = cloneValue.Id;
                clone.Values.Add(cloneValue);
            }
            product.Options.Add(clone);
        }
        foreach (var media in source.Media)
            product.Media.Add(new ProductMedia
            {
                Product = product, MediaAssetId = media.MediaAssetId, Role = media.Role,
                AltTextOverride = media.AltTextOverride, DisplayOrder = media.DisplayOrder, CreatedAt = now
            });
        foreach (var variant in source.Variants)
        {
            var clone = new ProductVariant
            {
                Product = product, Name = variant.Name,
                Sku = await UniqueSku($"{variant.Sku}-COPY", null, cancellationToken),
                PriceOverrideMinor = variant.PriceOverrideMinor, LowStockThreshold = variant.LowStockThreshold,
                IsActive = variant.IsActive, OnHandQuantity = 0, ReservedQuantity = 0, CreatedAt = now
            };
            foreach (var link in variant.OptionValues)
                if (valueMap.TryGetValue(link.ProductOptionValueId, out var valueId))
                    clone.OptionValues.Add(new ProductVariantOptionValue { ProductVariant = clone, ProductOptionValueId = valueId });
            product.Variants.Add(clone);
        }
        CloneModifiersAndVisualizer(source, product, now, valueMap);
        dbContext.Products.Add(product);
        AddActivity(actorUserId, "store.product.duplicated", "Product", product.Id, $"Duplicated product '{source.Name}' as '{product.Name}'.");
        await SaveAsync(cancellationToken);
        return MapProduct((await ProductGraph(product.Id, cancellationToken))!);
    }

    public async Task<AdminStoreProductDto> RegenerateProductSlugAsync(
        Guid id,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw new CmsNotFoundException("Product", id);
        if (product.Status != StoreProductStatus.Draft)
            Invalid("Slug", "Only draft products can have a copied URL repaired.");
        if (!IsCopiedProductSlug(product.Slug))
            Invalid("Slug", "This product does not have a copied draft URL.");

        var oldSlug = product.Slug;
        var newSlug = await UniqueProductSlug(product.Name, product.Id, cancellationToken);
        if (string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase))
            Invalid("Slug", "Save a different product name before repairing its URL.");

        product.Slug = newSlug;
        product.UpdatedAt = clock.UtcNow;
        AddActivity(actorUserId, "store.product.slug-regenerated", "Product", product.Id,
            $"Regenerated product slug from '{oldSlug}' to '{newSlug}'.");
        await SaveAsync(cancellationToken);
        return MapProduct((await ProductGraph(product.Id, cancellationToken))!);
    }

    public async Task ArchiveProductAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw new CmsNotFoundException("Product", id);
        product.Status = StoreProductStatus.Archived;
        product.IsFeatured = false;
        product.UpdatedAt = clock.UtcNow;
        AddActivity(actorUserId, "store.product.archived", "Product", product.Id, $"Archived product '{product.Name}'.");
        await SaveAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.ProductCategories.AsNoTracking()
            .OrderBy(value => value.DisplayOrder).ThenBy(value => value.Name)
            .Select(value => new AdminProductCategoryDto(
                value.Id, value.Name, value.Slug, value.DisplayOrder, value.IsActive,
                value.Products.Count, value.SquareCatalogObjectId))
            .ToListAsync(cancellationToken);

    public async Task<AdminProductCategoryDto> CreateCategoryAsync(
        ProductCategoryWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ValidateName(request.Name, "Name");
        var category = new ProductCategory
        {
            Name = request.Name.Trim(),
            Slug = await UniqueCategorySlug(request.Name, null, cancellationToken),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = clock.UtcNow
        };
        dbContext.ProductCategories.Add(category);
        AddActivity(actorUserId, "store.category.created", "ProductCategory", category.Id, $"Created category '{category.Name}'.");
        await SaveAsync(cancellationToken);
        return new AdminProductCategoryDto(category.Id, category.Name, category.Slug, category.DisplayOrder, category.IsActive, 0, null);
    }

    public async Task<AdminProductCategoryDto> UpdateCategoryAsync(
        Guid id,
        ProductCategoryWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ValidateName(request.Name, "Name");
        var category = await dbContext.ProductCategories.SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw new CmsNotFoundException("Product category", id);
        category.Name = request.Name.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = clock.UtcNow;
        AddActivity(actorUserId, "store.category.updated", "ProductCategory", category.Id, $"Updated category '{category.Name}'.");
        await SaveAsync(cancellationToken);
        var count = await dbContext.Products.CountAsync(value => value.CategoryId == id, cancellationToken);
        return new AdminProductCategoryDto(category.Id, category.Name, category.Slug, category.DisplayOrder, category.IsActive, count, category.SquareCatalogObjectId);
    }

    public async Task<PagedResultDto<AdminInventoryVariantDto>> GetInventoryAsync(
        AdminInventoryOptions options,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = NormalizePage(options.Page, options.PageSize, 200);
        var query = dbContext.ProductVariants.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(value =>
                value.Product.Name.Contains(search) || value.Name.Contains(search) || value.Sku.Contains(search));
        }
        if (options.ProductId.HasValue) query = query.Where(value => value.ProductId == options.ProductId);
        if (options.IsActive.HasValue) query = query.Where(value => value.IsActive == options.IsActive);
        if (options.LowStock.HasValue)
            query = options.LowStock.Value
                ? query.Where(value => value.OnHandQuantity - value.ReservedQuantity <= value.LowStockThreshold)
                : query.Where(value => value.OnHandQuantity - value.ReservedQuantity > value.LowStockThreshold);

        var total = await query.CountAsync(cancellationToken);
        var variants = await query.Include(value => value.Product)
            .OrderBy(value => value.Product.Name).ThenBy(value => value.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = variants.Select(InventoryDto).ToList();
        return new PagedResultDto<AdminInventoryVariantDto>(items, page, pageSize, total);
    }

    public async Task<AdminInventoryVariantDto> AdjustInventoryAsync(
        Guid variantId,
        InventoryAdjustmentWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ValidateManualAdjustment(request.QuantityDelta, request.Reason);
        var variant = await InventoryVariant(variantId, cancellationToken);
        ApplyRowVersion(variant, request.RowVersion);
        ApplyInventoryDelta(variant, request.QuantityDelta, request.Reason, request.Note, actorUserId, clock.UtcNow);
        AddActivity(actorUserId, "store.inventory.adjusted", "ProductVariant", variant.Id, $"Adjusted SKU '{variant.Sku}' by {request.QuantityDelta}.");
        await SaveAsync(cancellationToken);
        return InventoryDto(variant);
    }

    public async Task<IReadOnlyList<AdminInventoryVariantDto>> ReceiveInventoryAsync(
        BulkInventoryReceiptDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.Lines.Count is < 1 or > 500 || request.Lines.Any(value => value.Quantity <= 0))
            Invalid("Lines", "Provide between 1 and 500 receipt lines with positive quantities.");
        if (request.Lines.Select(value => value.VariantId).Distinct().Count() != request.Lines.Count)
            Invalid("Lines", "Each variant can appear only once.");
        var ids = request.Lines.Select(value => value.VariantId).ToHashSet();
        var variants = await dbContext.ProductVariants.Include(value => value.Product)
            .Where(value => ids.Contains(value.Id)).ToListAsync(cancellationToken);
        if (variants.Count != ids.Count) throw new CmsConflictException("One or more inventory variants no longer exist.");
        var byId = variants.ToDictionary(value => value.Id);
        var now = clock.UtcNow;
        foreach (var line in request.Lines)
        {
            var variant = byId[line.VariantId];
            ApplyRowVersion(variant, line.RowVersion);
            ApplyInventoryDelta(variant, line.Quantity, InventoryAdjustmentReason.Receipt, request.Note, actorUserId, now);
        }
        AddActivity(actorUserId, "store.inventory.received", "Inventory", null, $"Received stock for {variants.Count} variants.");
        await SaveAsync(cancellationToken);
        return variants.OrderBy(value => value.Product.Name).ThenBy(value => value.Name).Select(InventoryDto).ToList();
    }

    public async Task<AdminInventoryStocktakeDto> CompleteStocktakeAsync(
        InventoryStocktakeWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.Lines.Count is < 1 or > 1000 || request.Lines.Any(value => value.CountedOnHandQuantity < 0))
            Invalid("Lines", "Provide between 1 and 1,000 stocktake lines with non-negative counts.");
        if (request.Lines.Select(value => value.VariantId).Distinct().Count() != request.Lines.Count)
            Invalid("Lines", "Each variant can appear only once.");
        var ids = request.Lines.Select(value => value.VariantId).ToHashSet();
        var variants = await dbContext.ProductVariants.Include(value => value.Product)
            .Where(value => ids.Contains(value.Id)).ToListAsync(cancellationToken);
        if (variants.Count != ids.Count) throw new CmsConflictException("One or more inventory variants no longer exist.");
        var byId = variants.ToDictionary(value => value.Id);
        var now = clock.UtcNow;
        var stocktake = new InventoryStocktake
        {
            ActorUserId = actorUserId, Note = Clean(request.Note), VariantCount = request.Lines.Count, CreatedAt = now
        };
        foreach (var line in request.Lines)
        {
            var variant = byId[line.VariantId];
            ApplyRowVersion(variant, line.RowVersion);
            if (line.CountedOnHandQuantity < variant.ReservedQuantity)
                throw new CmsConflictException($"The count for SKU '{variant.Sku}' cannot be below its reserved quantity.");
            var delta = line.CountedOnHandQuantity - variant.OnHandQuantity;
            InventoryAdjustment? adjustment = null;
            if (delta != 0)
            {
                adjustment = ApplyInventoryDelta(variant, delta, InventoryAdjustmentReason.Correction, request.Note, actorUserId, now);
                stocktake.ChangedVariantCount++;
            }
            stocktake.Lines.Add(new InventoryStocktakeLine
            {
                ProductVariantId = variant.Id,
                ExpectedOnHandQuantity = line.CountedOnHandQuantity - delta,
                CountedOnHandQuantity = line.CountedOnHandQuantity,
                InventoryAdjustment = adjustment,
                CreatedAt = now
            });
        }
        dbContext.InventoryStocktakes.Add(stocktake);
        AddActivity(actorUserId, "store.inventory.stocktake", "InventoryStocktake", stocktake.Id, $"Completed a stocktake for {stocktake.VariantCount} variants.");
        await SaveAsync(cancellationToken);
        return StocktakeDto(stocktake, await ActorName(actorUserId, cancellationToken));
    }

    public async Task<PagedResultDto<AdminInventoryAdjustmentDto>> GetInventoryHistoryAsync(
        Guid? variantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize, 200);
        var query = dbContext.InventoryAdjustments.AsNoTracking().AsQueryable();
        if (variantId.HasValue) query = query.Where(value => value.ProductVariantId == variantId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(value => value.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(value => new AdminInventoryAdjustmentDto(
                value.Id, value.ProductVariantId, value.ProductVariant.Product.Name,
                value.ProductVariant.Name, value.ProductVariant.Sku, value.Reason,
                value.QuantityDelta, value.ResultingOnHandQuantity, value.Note,
                value.ActorUser == null ? "System" : value.ActorUser.FirstName + " " + value.ActorUser.LastName,
                value.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<AdminInventoryAdjustmentDto>(items, page, pageSize, total);
    }

    public async Task<PagedResultDto<AdminInventoryStocktakeDto>> GetStocktakesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize, 100);
        var query = dbContext.InventoryStocktakes.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(value => value.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(value => new AdminInventoryStocktakeDto(
                value.Id, value.VariantCount, value.ChangedVariantCount, value.Note,
                value.ActorUser.FirstName + " " + value.ActorUser.LastName, value.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<AdminInventoryStocktakeDto>(items, page, pageSize, total);
    }

    private async Task<Product?> ProductGraph(Guid id, CancellationToken token) =>
        await dbContext.Products
            .Include(value => value.Category)
            .Include(value => value.Media).ThenInclude(value => value.MediaAsset)
            .Include(value => value.Options).ThenInclude(value => value.Values)
            .Include(value => value.Variants).ThenInclude(value => value.OptionValues)
            .Include(value => value.ModifierGroups).ThenInclude(value => value.Values)
            .Include(value => value.VisualizerLayers)
            .SingleOrDefaultAsync(value => value.Id == id, token);

    private async Task<ProductVariant> InventoryVariant(Guid id, CancellationToken token) =>
        await dbContext.ProductVariants.Include(value => value.Product)
            .SingleOrDefaultAsync(value => value.Id == id, token)
        ?? throw new CmsNotFoundException("Product variant", id);

    private void ApplyNewGraph(Product product, StoreProductWriteDto request, DateTimeOffset now)
    {
        foreach (var media in request.Media)
            product.Media.Add(new ProductMedia
            {
                Id = media.Id, MediaAssetId = media.MediaAssetId, Role = media.Role,
                AltTextOverride = Clean(media.AltTextOverride), DisplayOrder = media.DisplayOrder, CreatedAt = now
            });
        foreach (var option in request.Options)
        {
            var entity = new ProductOption
            {
                Id = option.Id, Name = option.Name.Trim(), IsTracked = option.IsTracked,
                DisplayOrder = option.DisplayOrder, IsActive = option.IsActive, CreatedAt = now
            };
            foreach (var value in option.Values)
                entity.Values.Add(new ProductOptionValue
                {
                    Id = value.Id, Name = value.Name.Trim(), Slug = slugGenerator.Generate(value.Name),
                    ColorHex = Clean(value.ColorHex), SwatchMediaAssetId = value.SwatchMediaAssetId,
                    DisplayOrder = value.DisplayOrder, IsActive = value.IsActive, CreatedAt = now
                });
            product.Options.Add(entity);
        }
        foreach (var variant in request.Variants)
        {
            var entity = new ProductVariant
            {
                Id = variant.Id, Name = variant.Name.Trim(), Sku = variant.Sku.Trim(),
                PriceOverrideMinor = variant.PriceOverrideMinor, LowStockThreshold = variant.LowStockThreshold,
                IsActive = variant.IsActive, CreatedAt = now
            };
            foreach (var id in variant.OptionValueIds)
                entity.OptionValues.Add(new ProductVariantOptionValue { ProductVariant = entity, ProductOptionValueId = id });
            product.Variants.Add(entity);
        }
        foreach (var group in request.ModifierGroups) product.ModifierGroups.Add(NewModifierGroup(group, now));
        foreach (var layer in request.VisualizerLayers) product.VisualizerLayers.Add(NewLayer(layer, now));
    }

    private void SyncMedia(Product product, IReadOnlyList<ProductMediaWriteDto> requested, DateTimeOffset now)
    {
        dbContext.ProductMedia.RemoveRange(product.Media.Where(value => requested.All(item => item.Id != value.Id)));
        foreach (var item in requested)
        {
            var entity = product.Media.SingleOrDefault(value => value.Id == item.Id);
            if (entity == null)
            {
                entity = new ProductMedia
                {
                    Id = item.Id, Product = product, MediaAssetId = item.MediaAssetId, Role = item.Role,
                    AltTextOverride = Clean(item.AltTextOverride), DisplayOrder = item.DisplayOrder, CreatedAt = now
                };
                product.Media.Add(entity);
                dbContext.ProductMedia.Add(entity);
            }
            else
            {
                entity.MediaAssetId = item.MediaAssetId; entity.Role = item.Role;
                entity.AltTextOverride = Clean(item.AltTextOverride); entity.DisplayOrder = item.DisplayOrder; entity.UpdatedAt = now;
            }
        }
    }

    private void SyncOptions(Product product, IReadOnlyList<ProductOptionWriteDto> requested, DateTimeOffset now)
    {
        foreach (var existing in product.Options.Where(value => requested.All(item => item.Id != value.Id)))
        {
            existing.IsActive = false;
            foreach (var value in existing.Values) value.IsActive = false;
        }
        foreach (var item in requested)
        {
            var option = product.Options.SingleOrDefault(value => value.Id == item.Id);
            if (option == null)
            {
                option = new ProductOption { Id = item.Id, Product = product, CreatedAt = now };
                product.Options.Add(option);
                dbContext.ProductOptions.Add(option);
            }
            option.Name = item.Name.Trim(); option.IsTracked = item.IsTracked;
            option.DisplayOrder = item.DisplayOrder; option.IsActive = item.IsActive; option.UpdatedAt = now;
            foreach (var existing in option.Values.Where(value => item.Values.All(requestValue => requestValue.Id != value.Id)))
                existing.IsActive = false;
            foreach (var value in item.Values)
            {
                var entity = option.Values.SingleOrDefault(existing => existing.Id == value.Id);
                if (entity == null)
                {
                    entity = new ProductOptionValue { Id = value.Id, ProductOption = option, CreatedAt = now };
                    option.Values.Add(entity);
                    dbContext.ProductOptionValues.Add(entity);
                }
                entity.Name = value.Name.Trim(); entity.Slug = slugGenerator.Generate(value.Name);
                entity.ColorHex = Clean(value.ColorHex); entity.SwatchMediaAssetId = value.SwatchMediaAssetId;
                entity.DisplayOrder = value.DisplayOrder; entity.IsActive = value.IsActive; entity.UpdatedAt = now;
            }
        }
    }

    private void SyncVariants(Product product, IReadOnlyList<ProductVariantWriteDto> requested, DateTimeOffset now)
    {
        foreach (var existing in product.Variants.Where(value => requested.All(item => item.Id != value.Id)))
            existing.IsActive = false;
        foreach (var item in requested)
        {
            var variant = product.Variants.SingleOrDefault(value => value.Id == item.Id);
            if (variant == null)
            {
                variant = new ProductVariant { Id = item.Id, Product = product, CreatedAt = now };
                product.Variants.Add(variant);
                dbContext.ProductVariants.Add(variant);
            }
            else if (!string.IsNullOrWhiteSpace(item.RowVersion))
            {
                ApplyRowVersion(variant, item.RowVersion);
            }
            variant.Name = item.Name.Trim(); variant.Sku = item.Sku.Trim();
            variant.PriceOverrideMinor = item.PriceOverrideMinor; variant.LowStockThreshold = item.LowStockThreshold;
            variant.IsActive = item.IsActive; variant.UpdatedAt = now;
            var requestedValues = item.OptionValueIds.ToHashSet();
            foreach (var link in variant.OptionValues.Where(link => !requestedValues.Contains(link.ProductOptionValueId)).ToList())
            {
                dbContext.ProductVariantOptionValues.Remove(link);
                variant.OptionValues.Remove(link);
            }
            var existingValues = variant.OptionValues.Select(link => link.ProductOptionValueId).ToHashSet();
            foreach (var valueId in requestedValues.Where(valueId => !existingValues.Contains(valueId)))
            {
                var link = new ProductVariantOptionValue { ProductVariant = variant, ProductOptionValueId = valueId };
                variant.OptionValues.Add(link);
                dbContext.ProductVariantOptionValues.Add(link);
            }
        }
    }

    private void SyncModifiers(Product product, IReadOnlyList<ProductModifierGroupWriteDto> requested, DateTimeOffset now)
    {
        foreach (var existing in product.ModifierGroups.Where(value => requested.All(item => item.Id != value.Id)).ToList())
        {
            dbContext.ProductModifierGroups.Remove(existing);
            product.ModifierGroups.Remove(existing);
        }
        foreach (var item in requested)
        {
            var group = product.ModifierGroups.SingleOrDefault(value => value.Id == item.Id);
            if (group == null)
            {
                group = new ProductModifierGroup { Id = item.Id, Product = product, CreatedAt = now };
                product.ModifierGroups.Add(group);
                dbContext.ProductModifierGroups.Add(group);
            }
            group.Name = item.Name.Trim(); group.Type = item.Type; group.IsRequired = item.IsRequired;
            group.MinimumSelections = item.MinimumSelections; group.MaximumSelections = item.MaximumSelections;
            group.DisplayOrder = item.DisplayOrder; group.IsActive = item.IsActive; group.UpdatedAt = now;

            foreach (var existing in group.Values.Where(value => item.Values.All(requestValue => requestValue.Id != value.Id)).ToList())
            {
                dbContext.ProductModifierValues.Remove(existing);
                group.Values.Remove(existing);
            }
            foreach (var value in item.Values)
            {
                var entity = group.Values.SingleOrDefault(existing => existing.Id == value.Id);
                if (entity == null)
                {
                    entity = new ProductModifierValue { Id = value.Id, ProductModifierGroup = group, CreatedAt = now };
                    group.Values.Add(entity);
                    dbContext.ProductModifierValues.Add(entity);
                }
                entity.Name = value.Name.Trim(); entity.PriceAdjustmentMinor = value.PriceAdjustmentMinor;
                entity.ColorHex = Clean(value.ColorHex); entity.OverlayMediaAssetId = value.OverlayMediaAssetId;
                entity.DisplayOrder = value.DisplayOrder; entity.IsActive = value.IsActive; entity.UpdatedAt = now;
            }
        }
    }

    private void SyncVisualizerLayers(Product product, IReadOnlyList<ProductVisualizerLayerWriteDto> requested, DateTimeOffset now)
    {
        foreach (var existing in product.VisualizerLayers.Where(value => requested.All(item => item.Id != value.Id)).ToList())
        {
            dbContext.ProductVisualizerLayers.Remove(existing);
            product.VisualizerLayers.Remove(existing);
        }
        foreach (var item in requested)
        {
            var layer = product.VisualizerLayers.SingleOrDefault(value => value.Id == item.Id);
            if (layer == null)
            {
                layer = new ProductVisualizerLayer { Id = item.Id, Product = product, CreatedAt = now };
                product.VisualizerLayers.Add(layer);
                dbContext.ProductVisualizerLayers.Add(layer);
            }
            layer.MediaAssetId = item.MediaAssetId; layer.ProductOptionValueId = item.ProductOptionValueId;
            layer.ProductModifierValueId = item.ProductModifierValueId; layer.XPercent = item.XPercent;
            layer.YPercent = item.YPercent; layer.WidthPercent = item.WidthPercent;
            layer.HeightPercent = item.HeightPercent; layer.ZIndex = item.ZIndex;
            layer.BlendMode = item.BlendMode.Trim(); layer.UpdatedAt = now;
        }
    }

    private static ProductModifierGroup NewModifierGroup(ProductModifierGroupWriteDto value, DateTimeOffset now)
    {
        var group = new ProductModifierGroup
        {
            Id = value.Id, Name = value.Name.Trim(), Type = value.Type, IsRequired = value.IsRequired,
            MinimumSelections = value.MinimumSelections, MaximumSelections = value.MaximumSelections,
            DisplayOrder = value.DisplayOrder, IsActive = value.IsActive, CreatedAt = now
        };
        foreach (var item in value.Values)
            group.Values.Add(new ProductModifierValue
            {
                Id = item.Id, Name = item.Name.Trim(), PriceAdjustmentMinor = item.PriceAdjustmentMinor,
                ColorHex = Clean(item.ColorHex), OverlayMediaAssetId = item.OverlayMediaAssetId,
                DisplayOrder = item.DisplayOrder, IsActive = item.IsActive, CreatedAt = now
            });
        return group;
    }

    private static ProductVisualizerLayer NewLayer(ProductVisualizerLayerWriteDto value, DateTimeOffset now) => new()
    {
        Id = value.Id, MediaAssetId = value.MediaAssetId, ProductOptionValueId = value.ProductOptionValueId,
        ProductModifierValueId = value.ProductModifierValueId, XPercent = value.XPercent, YPercent = value.YPercent,
        WidthPercent = value.WidthPercent, HeightPercent = value.HeightPercent,
        ZIndex = value.ZIndex, BlendMode = value.BlendMode.Trim(), CreatedAt = now
    };

    private static void CloneModifiersAndVisualizer(
        Product source, Product target, DateTimeOffset now, IReadOnlyDictionary<Guid, Guid> optionValueMap)
    {
        var modifierMap = new Dictionary<Guid, Guid>();
        foreach (var group in source.ModifierGroups)
        {
            var clone = new ProductModifierGroup
            {
                Product = target, Name = group.Name, Type = group.Type, IsRequired = group.IsRequired,
                MinimumSelections = group.MinimumSelections, MaximumSelections = group.MaximumSelections,
                DisplayOrder = group.DisplayOrder, IsActive = group.IsActive, CreatedAt = now
            };
            foreach (var value in group.Values)
            {
                var cloneValue = new ProductModifierValue
                {
                    ProductModifierGroup = clone, Name = value.Name, PriceAdjustmentMinor = value.PriceAdjustmentMinor,
                    ColorHex = value.ColorHex, OverlayMediaAssetId = value.OverlayMediaAssetId,
                    DisplayOrder = value.DisplayOrder, IsActive = value.IsActive, CreatedAt = now
                };
                modifierMap[value.Id] = cloneValue.Id;
                clone.Values.Add(cloneValue);
            }
            target.ModifierGroups.Add(clone);
        }
        foreach (var layer in source.VisualizerLayers)
            target.VisualizerLayers.Add(new ProductVisualizerLayer
            {
                Product = target, MediaAssetId = layer.MediaAssetId,
                ProductOptionValueId = layer.ProductOptionValueId.HasValue && optionValueMap.TryGetValue(layer.ProductOptionValueId.Value, out var optionId) ? optionId : null,
                ProductModifierValueId = layer.ProductModifierValueId.HasValue && modifierMap.TryGetValue(layer.ProductModifierValueId.Value, out var modifierId) ? modifierId : null,
                XPercent = layer.XPercent, YPercent = layer.YPercent, WidthPercent = layer.WidthPercent,
                HeightPercent = layer.HeightPercent, ZIndex = layer.ZIndex, BlendMode = layer.BlendMode, CreatedAt = now
            });
    }

    private async Task ValidateProductWrite(StoreProductWriteDto request, Guid? productId, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors["Name"] = ["A product name is required."];
        if (request.CategoryId.HasValue &&
            !await dbContext.ProductCategories.AnyAsync(value => value.Id == request.CategoryId && value.IsActive, token))
            errors["CategoryId"] = ["Choose an active category."];
        if (request.Variants.Select(value => value.Id).Distinct().Count() != request.Variants.Count ||
            request.Options.SelectMany(value => value.Values).Select(value => value.Id).Distinct().Count() !=
            request.Options.SelectMany(value => value.Values).Count())
            errors["Variants"] = ["Product option and variant identifiers must be unique."];
        if (request.Options.Any(value => value.IsActive && !value.IsTracked))
            errors["Options"] = ["Active product options define physical inventory and must be tracked. Add logo color, logo treatment, name, or number under Customizations instead."];
        if (request.Variants.Select(value => value.Sku.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Variants.Count)
            errors["Variants"] = ["Variant SKUs must be unique within the product."];
        var allowedValueIds = request.Options.SelectMany(value => value.Values).Select(value => value.Id).ToHashSet();
        if (request.Variants.Any(value =>
                string.IsNullOrWhiteSpace(value.Name) || string.IsNullOrWhiteSpace(value.Sku) ||
                value.LowStockThreshold < 0 || value.OptionValueIds.Any(id => !allowedValueIds.Contains(id))))
            errors["Variants"] = ["Every variant needs a name, SKU, non-negative threshold, and valid option values."];
        if (request.Status == StoreProductStatus.Published && !request.Variants.Any(value => value.IsActive))
            errors["Status"] = ["A published product must have at least one active variant."];
        if (request.Status == StoreProductStatus.Published && request.Media.Count == 0)
            errors["Media"] = ["A published product must have at least one image."];
        if (request.ModifierGroups.Any(value =>
                string.IsNullOrWhiteSpace(value.Name) || value.MinimumSelections < 0 ||
                value.MaximumSelections < value.MinimumSelections))
            errors["ModifierGroups"] = ["Modifier selection rules are invalid."];
        var allowedModifierValueIds = request.ModifierGroups
            .SelectMany(value => value.Values)
            .Select(value => value.Id)
            .ToHashSet();
        if (request.VisualizerLayers.Any(value =>
                value.XPercent is < 0 or > 100 || value.YPercent is < 0 or > 100 ||
                value.WidthPercent is <= 0 or > 100 || value.HeightPercent is <= 0 or > 100 ||
                value.ProductOptionValueId.HasValue && !allowedValueIds.Contains(value.ProductOptionValueId.Value) ||
                value.ProductModifierValueId.HasValue && !allowedModifierValueIds.Contains(value.ProductModifierValueId.Value)) ||
            request.VisualizerLayers.Select(value => value.Id).Distinct().Count() != request.VisualizerLayers.Count)
            errors["VisualizerLayers"] = ["Visualizer layers need unique identifiers, valid conditions, and placement values within the 0–100 percent canvas."];
        foreach (var sku in request.Variants.Select(value => value.Sku.Trim()).Where(value => value.Length > 0))
            if (await dbContext.ProductVariants.AnyAsync(value => value.Sku == sku && value.ProductId != productId, token))
                errors["Variants"] = [$"SKU '{sku}' is already in use."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
    }

    private static void ValidateManualAdjustment(int delta, InventoryAdjustmentReason reason)
    {
        if (delta == 0) Invalid("QuantityDelta", "The inventory change cannot be zero.");
        if (reason is InventoryAdjustmentReason.Sale or InventoryAdjustmentReason.ReservationRelease or InventoryAdjustmentReason.ReturnWithoutRestock)
            Invalid("Reason", "This adjustment reason is reserved for automated order workflows.");
        if (reason is InventoryAdjustmentReason.Receipt or InventoryAdjustmentReason.ReturnRestock && delta < 1)
            Invalid("QuantityDelta", "Receipts and restocks must increase inventory.");
        if (reason == InventoryAdjustmentReason.Damage && delta > -1)
            Invalid("QuantityDelta", "A damage adjustment must decrease inventory.");
    }

    private InventoryAdjustment ApplyInventoryDelta(
        ProductVariant variant, int delta, InventoryAdjustmentReason reason,
        string? note, Guid actorUserId, DateTimeOffset now)
    {
        var resulting = checked(variant.OnHandQuantity + delta);
        if (resulting < variant.ReservedQuantity)
            throw new CmsConflictException($"The adjustment would make SKU '{variant.Sku}' lower than its reserved quantity.");
        variant.OnHandQuantity = resulting;
        variant.UpdatedAt = now;
        var adjustment = new InventoryAdjustment
        {
            ProductVariant = variant, ActorUserId = actorUserId, Reason = reason,
            QuantityDelta = delta, ResultingOnHandQuantity = resulting, Note = Clean(note), CreatedAt = now
        };
        dbContext.InventoryAdjustments.Add(adjustment);
        return adjustment;
    }

    private void ApplyRowVersion(ProductVariant variant, string encoded)
    {
        byte[] rowVersion;
        try { rowVersion = Convert.FromBase64String(encoded); }
        catch (FormatException) { throw new CmsConflictException("Inventory changed or the concurrency token is invalid. Refresh and try again."); }
        dbContext.Entry(variant).Property(value => value.RowVersion).OriginalValue = rowVersion;
    }

    private AdminStoreProductDto MapProduct(Product value) => new(
        value.Id, value.CategoryId, value.Name, value.Slug, value.ShortDescription, value.Description,
        value.BasePriceMinor, value.Currency, value.Status, value.IsFeatured, value.DisplayOrder,
        value.AllowsSpecialRequests, value.SquareCatalogObjectId, value.SquareCatalogVersion,
        value.ImportedAtUtc,
        value.Media.OrderBy(media => media.DisplayOrder).Select(media => new AdminProductMediaDto(
            media.Id, media.MediaAssetId, media.MediaAsset.PublicUrl, media.MediaAsset.Title,
            media.MediaAsset.AltText, media.Role, media.AltTextOverride, media.DisplayOrder)).ToList(),
        value.Options.OrderBy(option => option.DisplayOrder).Select(option => new AdminProductOptionDto(
            option.Id, option.Name, option.IsTracked, option.DisplayOrder, option.IsActive, option.SquareCatalogObjectId,
            option.Values.OrderBy(item => item.DisplayOrder).Select(item => new AdminProductOptionValueDto(
                item.Id, item.Name, item.Slug, item.ColorHex, item.SwatchMediaAssetId,
                item.DisplayOrder, item.IsActive, item.SquareCatalogObjectId)).ToList())).ToList(),
        value.Variants.OrderBy(variant => variant.Name).Select(variant => new AdminProductVariantDto(
            variant.Id, variant.Name, variant.Sku, variant.PriceOverrideMinor, variant.OnHandQuantity,
            variant.ReservedQuantity, variant.OnHandQuantity - variant.ReservedQuantity,
            variant.LowStockThreshold, variant.IsActive, variant.SquareCatalogObjectId,
            variant.SquareCatalogVersion, Convert.ToBase64String(variant.RowVersion),
            variant.OptionValues.Select(link => link.ProductOptionValueId).ToList())).ToList(),
        value.ModifierGroups.OrderBy(group => group.DisplayOrder).Select(group => new AdminProductModifierGroupDto(
            group.Id, group.Name, group.Type, group.IsRequired, group.MinimumSelections, group.MaximumSelections,
            group.DisplayOrder, group.IsActive,
            group.Values.OrderBy(item => item.DisplayOrder).Select(item => new AdminProductModifierValueDto(
                item.Id, item.Name, item.PriceAdjustmentMinor, item.ColorHex, item.OverlayMediaAssetId,
                item.DisplayOrder, item.IsActive)).ToList())).ToList(),
        value.VisualizerLayers.OrderBy(layer => layer.ZIndex).Select(layer => new AdminProductVisualizerLayerDto(
            layer.Id, layer.MediaAssetId, layer.ProductOptionValueId, layer.ProductModifierValueId,
            layer.XPercent, layer.YPercent, layer.WidthPercent, layer.HeightPercent, layer.ZIndex, layer.BlendMode)).ToList(),
        value.CreatedAt, value.UpdatedAt);

    private static AdminInventoryVariantDto InventoryDto(ProductVariant value)
    {
        var available = value.OnHandQuantity - value.ReservedQuantity;
        return new AdminInventoryVariantDto(
            value.ProductId, value.Product.Name, value.Id, value.Name, value.Sku,
            value.OnHandQuantity, value.ReservedQuantity, available, value.LowStockThreshold,
            available > 0 && available <= value.LowStockThreshold, available <= 0,
            value.IsActive, Convert.ToBase64String(value.RowVersion), value.UpdatedAt);
    }

    private static AdminInventoryStocktakeDto StocktakeDto(InventoryStocktake value, string actorName) =>
        new(value.Id, value.VariantCount, value.ChangedVariantCount, value.Note, actorName, value.CreatedAt);

    private async Task<string> UniqueProductSlug(string name, Guid? id, CancellationToken token) =>
        await slugGenerator.GenerateUniqueAsync(name,
            async (slug, ct) =>
                dbContext.Products.Local.Any(value => value.Slug == slug && value.Id != id) ||
                await dbContext.Products.AnyAsync(value => value.Slug == slug && value.Id != id, ct), token);

    private async Task<string> UniqueCategorySlug(string name, Guid? id, CancellationToken token) =>
        await slugGenerator.GenerateUniqueAsync(name,
            async (slug, ct) =>
                dbContext.ProductCategories.Local.Any(value => value.Slug == slug && value.Id != id) ||
                await dbContext.ProductCategories.AnyAsync(value => value.Slug == slug && value.Id != id, ct), token);

    private async Task<string> UniqueSku(string requested, Guid? id, CancellationToken token)
    {
        var basis = requested.Trim();
        var candidate = basis;
        var suffix = 2;
        while (dbContext.ProductVariants.Local.Any(value => value.Sku == candidate && value.Id != id) ||
               await dbContext.ProductVariants.AnyAsync(value => value.Sku == candidate && value.Id != id, token))
            candidate = $"{basis}-{suffix++}";
        return candidate;
    }

    private async Task<string> ActorName(Guid id, CancellationToken token)
    {
        var actor = await dbContext.Users.AsNoTracking().SingleAsync(value => value.Id == id, token);
        return $"{actor.FirstName} {actor.LastName}".Trim();
    }

    private void AddActivity(Guid actorUserId, string action, string targetType, Guid? targetId, string summary) =>
        dbContext.AdminActivityLogs.Add(new AdminActivityLog
        {
            ActorUserId = actorUserId, Action = action, TargetType = targetType,
            TargetId = targetId, Summary = summary, CreatedAt = clock.UtcNow
        });

    private async Task SaveAsync(CancellationToken token)
    {
        try { await dbContext.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException)
        {
            throw new CmsConflictException("This record changed while you were editing it. Refresh and try again.");
        }
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize, int maximum) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, maximum));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsCopiedProductSlug(string slug)
    {
        var copyIndex = slug.LastIndexOf("-copy", StringComparison.OrdinalIgnoreCase);
        if (copyIndex <= 0) return false;
        var suffix = slug[(copyIndex + 5)..];
        return suffix.Length == 0 ||
               suffix[0] == '-' &&
               int.TryParse(suffix[1..], out var number) && number >= 2;
    }

    private static void ValidateName(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) Invalid(field, "A name is required.");
    }

    private static void Invalid(string field, string message) =>
        throw new CmsRequestValidationException(new Dictionary<string, string[]> { [field] = [message] });
}
