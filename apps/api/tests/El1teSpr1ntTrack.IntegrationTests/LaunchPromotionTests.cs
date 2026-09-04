extern alias promotion;

using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;
using PromotionArguments = promotion::PromotionArguments;
using PromotionEngine = promotion::PromotionEngine;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class LaunchPromotionTests
{
    [Fact]
    public async Task Manifest_ExcludesPrivateTables_HashesMedia_AndImportsIdempotently()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"promotion-source-{Guid.NewGuid():N}");
        var destinationRoot = Path.Combine(Path.GetTempPath(), $"promotion-destination-{Guid.NewGuid():N}");
        try
        {
            await using var source = Context("source");
            var uploader = new User { Email = "demo@example.test", FirstName = "Demo", LastName = "Admin", PasswordHash = "not-exported" };
            var media = new MediaAsset { UploadedByUserId = uploader.Id, OriginalFileName = "team.png", StorageKey = "2026/08/team.png", ContentType = "image/png", FileExtension = ".png", FileSizeBytes = 4, Width = 1, Height = 1, Title = "Team", AltText = "Team", PublicUrl = $"https://demo-api.example/media/{Guid.NewGuid()}" };
            media.PublicUrl = $"https://demo-api.example/media/{media.Id}";
            source.AddRange(uploader, media,
                new Announcement { Title = "Published", Slug = "published", Summary = "Summary", Body = "Body", IsPublished = true, ImageUrl = media.PublicUrl },
                new Announcement { Title = "Draft", Slug = "draft", Summary = "Summary", Body = "Body", IsPublished = false },
                new ContactSubmission { Name = "Private", Email = "private@example.test", Message = "Do not export" });
            await source.SaveChangesAsync();
            var sourceStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = sourceRoot });
            await sourceStorage.SaveAsAsync(new MemoryStream([1, 2, 3, 4]), media.StorageKey, default);
            var args = Arguments(sourceRoot, destinationRoot);

            var manifest = await PromotionEngine.ExportAsync(source, args, sourceStorage);

            Assert.DoesNotContain(manifest.Records, value => value.Type is nameof(User) or nameof(ContactSubmission));
            Assert.Contains(manifest.Records, value => value.Type == nameof(Announcement) && value.Status == "Draft" && !value.Include);
            var mediaRecord = Assert.Single(manifest.Records, value => value.Type == nameof(MediaAsset));
            Assert.True(mediaRecord.Include);
            Assert.Equal(64, mediaRecord.BlobSha256!.Length);
            Assert.Contains(media.Id.ToString(), Assert.Single(manifest.Records, value => value.Type == nameof(Announcement) && value.Include).Dependencies);

            await using var destination = Context("destination");
            var destinationStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = destinationRoot });
            var first = await PromotionEngine.ImportAsync(destination, manifest, args with { Apply = true }, sourceStorage, destinationStorage);
            var second = await PromotionEngine.ImportAsync(destination, manifest, args with { Apply = true }, sourceStorage, destinationStorage);
            Assert.True(first.Inserted > 0);
            Assert.Equal(0, second.Inserted);
            Assert.True(second.Updated > 0);
            Assert.Equal(args.BootstrapUserId, (await destination.MediaAssets.SingleAsync()).UploadedByUserId);
            Assert.True(await destinationStorage.ExistsAsync(media.StorageKey, default));
        }
        finally
        {
            if (Directory.Exists(sourceRoot)) Directory.Delete(sourceRoot, true);
            if (Directory.Exists(destinationRoot)) Directory.Delete(destinationRoot, true);
        }
    }

    [Fact]
    public async Task Validation_RejectsASelectedRecordWhoseDependencyIsExcluded()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"promotion-source-{Guid.NewGuid():N}");
        var destinationRoot = Path.Combine(Path.GetTempPath(), $"promotion-destination-{Guid.NewGuid():N}");
        try
        {
            await using var source = Context("dependency-source");
            var uploader = new User { Email = "demo@example.test", FirstName = "Demo", LastName = "Admin", PasswordHash = "not-exported" };
            var media = new MediaAsset { UploadedByUserId = uploader.Id, OriginalFileName = "draft.png", StorageKey = "2026/08/draft.png", ContentType = "image/png", FileExtension = ".png", FileSizeBytes = 4, Width = 1, Height = 1, Title = "Draft", AltText = "Draft" };
            media.PublicUrl = $"https://demo-api.example/media/{media.Id}";
            source.AddRange(uploader, media, new Announcement { Title = "Draft", Slug = "draft", Summary = "Summary", Body = "Body", IsPublished = false, ImageUrl = media.PublicUrl });
            await source.SaveChangesAsync();
            var sourceStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = sourceRoot });
            await sourceStorage.SaveAsAsync(new MemoryStream([1, 2, 3, 4]), media.StorageKey, default);
            var args = Arguments(sourceRoot, destinationRoot);
            var manifest = await PromotionEngine.ExportAsync(source, args, sourceStorage);
            var selected = manifest with { Records = manifest.Records.Select(value => value.Type == nameof(Announcement) ? value with { Include = true } : value).ToList() };

            var error = Assert.Throws<InvalidOperationException>(() => PromotionEngine.Validate(selected, args));

            Assert.Contains("excluded dependencies", error.Message);
            Assert.Equal(64, Assert.Single(manifest.Records, value => value.Type == nameof(MediaAsset)).BlobSha256!.Length);
        }
        finally
        {
            if (Directory.Exists(sourceRoot)) Directory.Delete(sourceRoot, true);
            if (Directory.Exists(destinationRoot)) Directory.Delete(destinationRoot, true);
        }
    }

    [Fact]
    public async Task ProductSafeguards_IncludeCompleteDraftGraphs_AndForceSafeProductionState()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"promotion-source-{Guid.NewGuid():N}");
        var destinationRoot = Path.Combine(Path.GetTempPath(), $"promotion-destination-{Guid.NewGuid():N}");
        try
        {
            await using var source = Context("catalog-source");
            var uploader = new User { Email = "catalog@example.test", FirstName = "Catalog", LastName = "Admin", PasswordHash = "not-exported" };
            var media = new MediaAsset
            {
                UploadedByUserId = uploader.Id, OriginalFileName = "shirt.png", StorageKey = "catalog/shirt.png",
                ContentType = "image/png", FileExtension = ".png", FileSizeBytes = 4, Width = 1, Height = 1,
                Title = "Shirt", AltText = "Shirt", PublicUrl = string.Empty
            };
            media.PublicUrl = $"https://demo-api.example/media/{media.Id}";
            var derivative = new MediaDerivative
            {
                MediaAssetId = media.Id, RequestedWidth = 480, Width = 480, Height = 480,
                StorageKey = "catalog/shirt-480.webp", FileSizeBytes = 3, Sha256 = new string('A', 64)
            };
            var category = new ProductCategory { Name = "Apparel", Slug = "apparel" };
            var published = new Product
            {
                CategoryId = category.Id, Name = "Published Shirt", Slug = "published-shirt",
                Status = StoreProductStatus.Published, IsFeatured = true, BasePriceMinor = 2500
            };
            var draft = new Product
            {
                CategoryId = category.Id, Name = "Draft Shirt", Slug = "draft-shirt",
                Status = StoreProductStatus.Draft, IsFeatured = true, BasePriceMinor = 3000
            };
            var option = new ProductOption { ProductId = draft.Id, Name = "Size" };
            var optionValue = new ProductOptionValue { ProductOptionId = option.Id, Name = "Medium", Slug = "medium" };
            var variant = new ProductVariant
            {
                ProductId = draft.Id, Name = "Medium", Sku = "DRAFT-M", OnHandQuantity = 8,
                ReservedQuantity = 2, IsActive = true
            };
            var modifierGroup = new ProductModifierGroup { ProductId = draft.Id, Name = "Logo Color", IsRequired = true };
            var modifierValue = new ProductModifierValue { ProductModifierGroupId = modifierGroup.Id, Name = "Red" };
            source.AddRange(uploader, media, derivative, category, published, draft,
                new ProductMedia { ProductId = draft.Id, MediaAssetId = media.Id, Role = ProductMediaRole.Gallery },
                option, optionValue, variant,
                new ProductVariantOptionValue { ProductVariantId = variant.Id, ProductOptionValueId = optionValue.Id },
                modifierGroup, modifierValue,
                new ProductVisualizerLayer { ProductId = draft.Id, MediaAssetId = media.Id, ProductModifierValueId = modifierValue.Id },
                new AllAmericanYear { Year = 2026, Slug = "2026", Title = "Shelved archive", Summary = "Not for launch", IsPublished = true });
            await source.SaveChangesAsync();
            var sourceStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = sourceRoot });
            await sourceStorage.SaveAsAsync(new MemoryStream([1, 2, 3, 4]), media.StorageKey, default);
            await sourceStorage.SaveAsAsync(new MemoryStream([5, 6, 7]), derivative.StorageKey, default);

            var defaultManifest = await PromotionEngine.ExportAsync(source, Arguments(sourceRoot, destinationRoot), sourceStorage);
            Assert.Single(defaultManifest.Records, value => value.Type == nameof(Product) && value.Include);
            Assert.DoesNotContain(defaultManifest.Records, value => value.Type == nameof(ProductOption) && value.Include);

            var safeArgs = Arguments(sourceRoot, destinationRoot, includeAllProducts: true, forceProductsDraft: true);
            var completeManifest = await PromotionEngine.ExportAsync(source, safeArgs, sourceStorage);
            Assert.Equal(2, completeManifest.Records.Count(value => value.Type == nameof(Product) && value.Include));
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductOption) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductOptionValue) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductVariant) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductVariantOptionValue) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductModifierGroup) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductModifierValue) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductMedia) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(ProductVisualizerLayer) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(MediaAsset) && value.Include);
            Assert.Contains(completeManifest.Records, value => value.Type == nameof(MediaDerivative) && value.Include);

            var launchManifest = completeManifest with
            {
                Records = completeManifest.Records.Select(value => value.Type.StartsWith("AllAmerican", StringComparison.Ordinal)
                    ? value with { Include = false }
                    : value).ToList()
            };
            PromotionEngine.Validate(launchManifest, safeArgs);

            await using var destination = Context("catalog-destination");
            var destinationStorage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = destinationRoot });
            var first = await PromotionEngine.ImportAsync(destination, launchManifest, safeArgs with { Apply = true }, sourceStorage, destinationStorage);
            var second = await PromotionEngine.ImportAsync(destination, launchManifest, safeArgs with { Apply = true }, sourceStorage, destinationStorage);

            Assert.Equal(2, first.Counts[nameof(Product)]);
            Assert.Equal(0, second.Inserted);
            Assert.All(await destination.Products.ToListAsync(), product =>
            {
                Assert.Equal(StoreProductStatus.Draft, product.Status);
                Assert.False(product.IsFeatured);
            });
            var importedVariant = await destination.ProductVariants.SingleAsync();
            Assert.Equal(0, importedVariant.OnHandQuantity);
            Assert.Equal(0, importedVariant.ReservedQuantity);
            Assert.Empty(await destination.AllAmericanYears.ToListAsync());
            Assert.True(await destinationStorage.ExistsAsync(media.StorageKey, default));
            Assert.True(await destinationStorage.ExistsAsync(derivative.StorageKey, default));
        }
        finally
        {
            if (Directory.Exists(sourceRoot)) Directory.Delete(sourceRoot, true);
            if (Directory.Exists(destinationRoot)) Directory.Delete(destinationRoot, true);
        }
    }

    private static El1teDbContext Context(string suffix) => new(new DbContextOptionsBuilder<El1teDbContext>()
        .UseInMemoryDatabase($"promotion-{suffix}-{Guid.NewGuid():N}").Options);
    private static PromotionArguments Arguments(string sourceRoot, string destinationRoot,
        bool includeAllProducts = false, bool forceProductsDraft = false) => new(
        "export", "demo", "production", "unused", "unused", "manifest.json",
        "https://demo-api.example", "https://api.example", false, "", Guid.NewGuid(),
        sourceRoot, destinationRoot, "", "", "media", includeAllProducts, forceProductsDraft);
}
