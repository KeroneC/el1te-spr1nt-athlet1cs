using System.Net;
using System.Text.RegularExpressions;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed partial class PrintifyAdminService(
    El1teDbContext dbContext,
    IPrintifyClient client,
    IPrintifyCatalogImageImporter imageImporter,
    ISlugGenerator slugGenerator,
    IClock clock,
    PrintifySettings settings) : IPrintifyAdminService
{
    private static readonly string[] ExpectedTopics =
    [
        "order:created",
        "order:updated",
        "order:sent-to-production",
        "order:shipment:created",
        "order:shipment:delivered"
    ];

    public async Task<PrintifyIntegrationHealthDto> GetHealthAsync(CancellationToken cancellationToken)
    {
        PrintifyShopSnapshot? shop = null;
        IReadOnlyList<PrintifyWebhookSubscription> webhooks = [];
        var healthy = false;
        if (settings.HasCatalogCredentials)
        {
            try
            {
                shop = await client.GetShopAsync(cancellationToken);
                webhooks = await client.GetWebhooksAsync(cancellationToken);
                healthy = true;
            }
            catch (PrintifyIntegrationException)
            {
                healthy = false;
            }
        }

        var mappedProducts = await dbContext.Products.AsNoTracking()
            .Where(value => value.FulfillmentMode == ProductFulfillmentMode.PrintifyDirectShip)
            .Select(value => new
            {
                value.PrintifyLastSyncedAtUtc,
                Issue = value.PrintifyProductId == null ||
                        value.Variants.Any(variant => variant.IsActive && variant.PrintifyVariantId == null)
            })
            .ToListAsync(cancellationToken);
        var matchingWebhooks = webhooks.Count(value =>
            ExpectedTopics.Contains(value.Topic, StringComparer.Ordinal) &&
            string.Equals(value.Url, settings.WebhookNotificationUrl, StringComparison.OrdinalIgnoreCase));
        var now = clock.UtcNow;
        return new PrintifyIntegrationHealthDto(
            settings.Enabled,
            settings.HasCatalogCredentials,
            healthy,
            shop?.Title,
            settings.ShopId,
            settings.TokenExpiresAtUtc,
            settings.TokenExpiresAtUtc.HasValue && settings.TokenExpiresAtUtc <= now.AddDays(30),
            settings.HasWebhookCredentials && Uri.TryCreate(settings.WebhookNotificationUrl, UriKind.Absolute, out _),
            ExpectedTopics.Length,
            matchingWebhooks,
            mappedProducts.Count,
            mappedProducts.Count(value => value.Issue),
            await dbContext.ProductVariants.CountAsync(
                value => value.Product.FulfillmentMode == ProductFulfillmentMode.PrintifyDirectShip &&
                         value.PrintifyVariantId != null && value.PrintifyProviderCostMinor == null,
                cancellationToken),
            mappedProducts.Max(value => value.PrintifyLastSyncedAtUtc),
            settings.MinimumGrossContributionMinor,
            settings.OrderCreationEnabled,
            settings.ProductionReleaseEnabled);
    }

    public async Task<PrintifyCatalogPreviewDto> PreviewAsync(CancellationToken cancellationToken)
    {
        if (!settings.HasCatalogCredentials)
        {
            return new PrintifyCatalogPreviewDto(false, null, 0, 0, []);
        }

        var shop = await client.GetShopAsync(cancellationToken);
        var products = await client.GetProductsAsync(cancellationToken);
        var ids = products.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        var connected = await dbContext.Products.AsNoTracking()
            .Where(value => value.PrintifyProductId != null && ids.Contains(value.PrintifyProductId))
            .Select(value => value.PrintifyProductId!)
            .ToHashSetAsync(cancellationToken);
        var rows = products.OrderBy(value => value.Title).Select(value => new PrintifyCatalogPreviewProductDto(
            value.Id,
            value.Title,
            value.Variants.Count,
            value.Variants.Count(variant => variant.IsEnabled && variant.IsAvailable),
            value.Images.Count,
            connected.Contains(value.Id))).ToList();
        return new PrintifyCatalogPreviewDto(true, shop.Title, rows.Count, rows.Count(value => !value.AlreadyConnected), rows);
    }

    public async Task<PrintifyCatalogImportResultDto> ImportAsync(
        PrintifyCatalogImportRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var requestedIds = request.ProductIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (requestedIds.Count is < 1 or > 100 || requestedIds.Any(value => value.Length > 100))
        {
            throw new CmsValidationException(new Dictionary<string, string[]>
            {
                ["ProductIds"] = ["Select between 1 and 100 valid Printify products."]
            });
        }

        var now = clock.UtcNow;
        var run = new PrintifyCatalogImportRun { ActorUserId = actorUserId, CreatedAt = now };
        dbContext.PrintifyCatalogImportRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        try
        {
            var available = (await client.GetProductsAsync(cancellationToken))
                .Where(value => requestedIds.Contains(value.Id, StringComparer.Ordinal))
                .ToDictionary(value => value.Id, StringComparer.Ordinal);
            run.ProductsDiscovered = available.Count;
            foreach (var id in requestedIds)
            {
                if (!available.TryGetValue(id, out var source))
                {
                    run.ProductsSkipped++;
                    continue;
                }
                if (await dbContext.Products.AnyAsync(value => value.PrintifyProductId == id, cancellationToken))
                {
                    run.ProductsSkipped++;
                    continue;
                }

                var product = await BuildProductAsync(source, actorUserId, now, cancellationToken);
                dbContext.Products.Add(product);
                run.ProductsCreated++;
                run.ImagesImported += product.Media.Count;
            }

            run.Status = PrintifyCatalogImportStatus.Completed;
            run.CompletedAtUtc = clock.UtcNow;
            AddActivity(actorUserId, "store.printify.imported", run.Id, $"Imported {run.ProductsCreated} Printify products as drafts.");
            await dbContext.SaveChangesAsync(cancellationToken);
            return new PrintifyCatalogImportResultDto(
                run.Id,
                run.ProductsDiscovered,
                run.ProductsCreated,
                run.ProductsSkipped,
                run.ImagesImported);
        }
        catch
        {
            dbContext.ChangeTracker.Clear();
            var failed = await dbContext.PrintifyCatalogImportRuns.SingleAsync(value => value.Id == run.Id, CancellationToken.None);
            failed.Status = PrintifyCatalogImportStatus.Failed;
            failed.SafeFailureCode = "printify_catalog_import_failed";
            failed.CompletedAtUtc = clock.UtcNow;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PrintifyRefreshResultDto> RefreshMappingsAsync(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .Include(value => value.Variants)
            .Where(value => value.FulfillmentMode == ProductFulfillmentMode.PrintifyDirectShip &&
                            value.PrintifyProductId != null)
            .ToListAsync(cancellationToken);
        var variantCount = 0;
        var availabilityChanges = 0;
        var costChanges = 0;
        var mappingIssues = 0;
        var now = clock.UtcNow;
        foreach (var product in products)
        {
            PrintifyProductSnapshot source;
            try
            {
                source = await client.GetProductAsync(product.PrintifyProductId!, cancellationToken);
            }
            catch (PrintifyIntegrationException)
            {
                mappingIssues++;
                continue;
            }

            var providerVariants = source.Variants.ToDictionary(value => value.Id);
            foreach (var variant in product.Variants.Where(value => value.IsActive))
            {
                variantCount++;
                if (!variant.PrintifyVariantId.HasValue ||
                    !providerVariants.TryGetValue(variant.PrintifyVariantId.Value, out var providerVariant))
                {
                    mappingIssues++;
                    continue;
                }
                var available = providerVariant.IsEnabled && providerVariant.IsAvailable;
                if (variant.PrintifyIsAvailable != available) availabilityChanges++;
                if (variant.PrintifyProviderCostMinor != providerVariant.ProviderCostMinor) costChanges++;
                variant.PrintifyIsAvailable = available;
                variant.PrintifyProviderCostMinor = providerVariant.ProviderCostMinor;
                variant.PrintifyLastVerifiedAtUtc = now;
                variant.UpdatedAt = now;
            }

            product.PrintifyBlueprintId = source.BlueprintId;
            product.PrintifyProviderId = source.PrintProviderId;
            product.PrintifyLastSyncedAtUtc = now;
            product.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PrintifyRefreshResultDto(products.Count, variantCount, availabilityChanges, costChanges, mappingIssues, now);
    }

    private async Task<Product> BuildProductAsync(
        PrintifyProductSnapshot source,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = source.Title.Trim(),
            Slug = await UniqueSlugAsync(source.Title, cancellationToken),
            ShortDescription = CleanDescription(source.Description),
            BasePriceMinor = Math.Max(0, source.Variants.Where(value => value.IsEnabled)
                .Select(value => value.ProviderCostMinor).DefaultIfEmpty(0).Max() + settings.MinimumGrossContributionMinor),
            Currency = "USD",
            Status = StoreProductStatus.Draft,
            FulfillmentMode = ProductFulfillmentMode.PrintifyDirectShip,
            AllowsSpecialRequests = false,
            PrintifyProductId = source.Id,
            PrintifyBlueprintId = source.BlueprintId,
            PrintifyProviderId = source.PrintProviderId,
            PrintifyLastSyncedAtUtc = now,
            ImportedAtUtc = now,
            CreatedAt = now
        };
        var optionValues = new Dictionary<int, ProductOptionValue>();
        foreach (var sourceOption in source.Options.OrderBy(value => value.DisplayOrder))
        {
            var option = new ProductOption
            {
                Name = sourceOption.Name,
                IsTracked = true,
                DisplayOrder = sourceOption.DisplayOrder,
                IsActive = true,
                CreatedAt = now
            };
            foreach (var sourceValue in sourceOption.Values.OrderBy(value => value.DisplayOrder))
            {
                var value = new ProductOptionValue
                {
                    Name = sourceValue.Title,
                    Slug = slugGenerator.Generate(sourceValue.Title),
                    ColorHex = NormalizeColor(sourceValue.ColorHex),
                    DisplayOrder = sourceValue.DisplayOrder,
                    IsActive = true,
                    CreatedAt = now
                };
                option.Values.Add(value);
                optionValues[sourceValue.Id] = value;
            }
            product.Options.Add(option);
        }

        foreach (var sourceVariant in source.Variants.OrderBy(value => value.Title))
        {
            var variant = new ProductVariant
            {
                Name = sourceVariant.Title,
                Sku = await UniqueSkuAsync(sourceVariant, cancellationToken),
                IsActive = sourceVariant.IsEnabled,
                OnHandQuantity = 0,
                ReservedQuantity = 0,
                LowStockThreshold = 0,
                PrintifyVariantId = sourceVariant.Id,
                PrintifyProviderCostMinor = sourceVariant.ProviderCostMinor,
                PrintifyIsAvailable = sourceVariant.IsEnabled && sourceVariant.IsAvailable,
                PrintifyLastVerifiedAtUtc = now,
                CreatedAt = now
            };
            foreach (var valueId in sourceVariant.OptionValueIds)
            {
                if (optionValues.TryGetValue(valueId, out var value))
                {
                    variant.OptionValues.Add(new ProductVariantOptionValue
                    {
                        ProductVariant = variant,
                        ProductOptionValue = value
                    });
                }
            }
            product.Variants.Add(variant);
        }

        foreach (var sourceImage in source.Images.OrderByDescending(value => value.IsDefault).Take(8))
        {
            var mediaId = await imageImporter.ImportAsync(sourceImage, source.Title, actorUserId, cancellationToken);
            if (mediaId.HasValue)
            {
                product.Media.Add(new ProductMedia
                {
                    MediaAssetId = mediaId.Value,
                    Role = ProductMediaRole.Gallery,
                    DisplayOrder = product.Media.Count,
                    CreatedAt = now
                });
            }
        }

        return product;
    }

    private async Task<string> UniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var root = slugGenerator.Generate(name);
        var slug = root;
        var suffix = 2;
        while (await dbContext.Products.AnyAsync(value => value.Slug == slug, cancellationToken))
        {
            slug = $"{root}-{suffix++}";
        }
        return slug;
    }

    private async Task<string> UniqueSkuAsync(PrintifyVariantSnapshot source, CancellationToken cancellationToken)
    {
        var root = string.IsNullOrWhiteSpace(source.Sku) ? $"PFY-{source.Id}" : source.Sku.Trim();
        root = root.Length > 90 ? root[..90] : root;
        var sku = root;
        var suffix = 2;
        while (await dbContext.ProductVariants.AnyAsync(value => value.Sku == sku, cancellationToken))
        {
            sku = $"{root}-{suffix++}";
        }
        return sku;
    }

    private void AddActivity(Guid actorUserId, string action, Guid targetId, string summary) =>
        dbContext.AdminActivityLogs.Add(new AdminActivityLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = "PrintifyCatalogImportRun",
            TargetId = targetId,
            Summary = summary,
            CreatedAt = clock.UtcNow
        });

    private static string? CleanDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var plain = WebUtility.HtmlDecode(HtmlTagPattern().Replace(description, " "));
        plain = WhitespacePattern().Replace(plain, " ").Trim();
        return plain.Length <= 500 ? plain : plain[..497] + "...";
    }

    private static string? NormalizeColor(string? color) =>
        !string.IsNullOrWhiteSpace(color) && ColorPattern().IsMatch(color) ? color.ToUpperInvariant() : null;

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();
}
