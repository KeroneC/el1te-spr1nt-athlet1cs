using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class PublicStoreServiceTests
{
    [Fact]
    public async Task Catalog_ExposesSafeAvailabilityAndOnlyPublishedProducts()
    {
        await using var db = Context();
        var category = new ProductCategory { Name = "Apparel", Slug = "apparel" };
        var published = new Product
        {
            Name = "Team hoodie", Slug = "team-hoodie", Status = StoreProductStatus.Published,
            Category = category, BasePriceMinor = 5000, Currency = "USD", IsFeatured = true
        };
        published.Variants.Add(new ProductVariant
        {
            Name = "Medium", Sku = "HOOD-M", OnHandQuantity = 4, ReservedQuantity = 2,
            LowStockThreshold = 2, IsActive = true
        });
        db.Products.AddRange(published, new Product
        {
            Name = "Hidden draft", Slug = "hidden-draft", Status = StoreProductStatus.Draft
        });
        await db.SaveChangesAsync();

        var result = await new PublicStoreService(db).GetProductsAsync(
            new(null, null, null), CancellationToken.None);

        var product = Assert.Single(result.Items);
        Assert.Equal("Team hoodie", product.Name);
        Assert.Equal(PublicStockStatus.LowStock, product.Availability);
        Assert.Equal(5000, product.MinimumPriceMinor);
        Assert.Single(result.Categories);
    }

    [Fact]
    public async Task Detail_MapsPurchasableConfigurationWithoutInventoryCountsOrSku()
    {
        await using var db = Context();
        var product = new Product
        {
            Name = "Team tee", Slug = "team-tee", Status = StoreProductStatus.Published,
            BasePriceMinor = 2500, Currency = "USD"
        };
        var option = new ProductOption { Product = product, Name = "Size", IsTracked = true, IsActive = true };
        var value = new ProductOptionValue { ProductOption = option, Name = "Medium", Slug = "medium", IsActive = true };
        var variant = new ProductVariant
        {
            Product = product, Name = "Medium", Sku = "PRIVATE-SKU", OnHandQuantity = 6,
            ReservedQuantity = 1, LowStockThreshold = 2, IsActive = true
        };
        variant.OptionValues.Add(new ProductVariantOptionValue { ProductVariant = variant, ProductOptionValue = value });
        db.AddRange(product, option, value, variant);
        await db.SaveChangesAsync();

        var result = await new PublicStoreService(db).GetProductAsync("team-tee", CancellationToken.None);

        Assert.NotNull(result);
        var publicVariant = Assert.Single(result.Variants);
        Assert.Equal(PublicStockStatus.InStock, publicVariant.Availability);
        Assert.Equal(2500, publicVariant.PriceMinor);
        Assert.Contains(value.Id, publicVariant.OptionValueIds);
        Assert.DoesNotContain("PRIVATE-SKU", System.Text.Json.JsonSerializer.Serialize(result));
        Assert.DoesNotContain("OnHand", System.Text.Json.JsonSerializer.Serialize(result));
        Assert.DoesNotContain("Reserved", System.Text.Json.JsonSerializer.Serialize(result));
    }

    private static El1teDbContext Context() => new(
        new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"public-store-{Guid.NewGuid():N}")
            .Options);
}
