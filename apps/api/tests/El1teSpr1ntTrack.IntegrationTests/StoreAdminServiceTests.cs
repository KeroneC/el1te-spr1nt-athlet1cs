using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Commerce;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class StoreAdminServiceTests
{
    [Fact]
    public async Task CreateAndDuplicateProduct_PreservesCatalogButResetsOperationalState()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var service = Service(db);
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var request = ProductRequest(optionId, valueId);

        var created = await service.CreateProductAsync(request, actor.Id, CancellationToken.None);
        var entity = await db.Products.Include(value => value.Variants).SingleAsync(value => value.Id == created.Id);
        entity.SquareCatalogObjectId = "SQUARE-PRODUCT";
        entity.SquareCatalogVersion = 12;
        entity.ImportedAtUtc = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        entity.Variants.Single().OnHandQuantity = 6;
        entity.Variants.Single().SquareCatalogObjectId = "SQUARE-VARIANT";
        entity.Variants.Single().SquareCatalogVersion = 13;
        await db.SaveChangesAsync();

        var duplicate = await service.DuplicateProductAsync(
            created.Id,
            new DuplicateProductWriteDto { Name = "  Warmup hoodie  " },
            actor.Id,
            CancellationToken.None);

        Assert.Equal("Warmup hoodie", duplicate.Name);
        Assert.Equal("warmup-hoodie", duplicate.Slug);
        Assert.Equal(StoreProductStatus.Draft, duplicate.Status);
        Assert.False(duplicate.IsFeatured);
        Assert.Null(duplicate.SquareCatalogObjectId);
        Assert.Null(duplicate.SquareCatalogVersion);
        Assert.Null(duplicate.ImportedAtUtc);
        var variant = Assert.Single(duplicate.Variants);
        Assert.Equal(0, variant.OnHandQuantity);
        Assert.Equal(0, variant.ReservedQuantity);
        Assert.Null(variant.SquareCatalogObjectId);
        Assert.Null(variant.SquareCatalogVersion);
        Assert.EndsWith("-COPY", variant.Sku);
        Assert.NotEqual(created.Options.Single().Values.Single().Id, duplicate.Options.Single().Values.Single().Id);
    }

    [Fact]
    public async Task DuplicateProduct_RejectsNamesLongerThanTwoHundredCharacters()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var service = Service(db);
        var created = await service.CreateProductAsync(
            ProductRequest(Guid.NewGuid(), Guid.NewGuid()), actor.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            service.DuplicateProductAsync(created.Id, new DuplicateProductWriteDto { Name = new string('x', 201) },
                actor.Id, CancellationToken.None));

        Assert.Contains("Name", exception.Errors);
        Assert.Single(await db.Products.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("team HOODIE")]
    public async Task DuplicateProduct_RejectsMissingOrUnchangedNames(string requestedName)
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var created = await Service(db).CreateProductAsync(
            ProductRequest(Guid.NewGuid(), Guid.NewGuid()), actor.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            Service(db).DuplicateProductAsync(created.Id, new DuplicateProductWriteDto { Name = requestedName },
                actor.Id, CancellationToken.None));

        Assert.Contains("Name", exception.Errors);
        Assert.Single(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task DuplicateProduct_GeneratesNumericSlugSuffixWhenRequestedNameCollides()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var service = Service(db);
        var source = await service.CreateProductAsync(
            ProductRequest(Guid.NewGuid(), Guid.NewGuid()), actor.Id, CancellationToken.None);
        db.Products.Add(new Product { Name = "Warmup hoodie", Slug = "warmup-hoodie" });
        await db.SaveChangesAsync();

        var duplicate = await service.DuplicateProductAsync(
            source.Id, new DuplicateProductWriteDto { Name = "Warmup hoodie" }, actor.Id, CancellationToken.None);

        Assert.Equal("warmup-hoodie-2", duplicate.Slug);
    }

    [Fact]
    public async Task RegenerateProductSlug_RepairsEligibleDraftAndRecordsOldAndNewSlug()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var product = new Product
        {
            Name = "Performance quarter zip", Slug = "team-hoodie-copy-2", Status = StoreProductStatus.Draft
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var updated = await Service(db).RegenerateProductSlugAsync(product.Id, actor.Id, CancellationToken.None);

        Assert.Equal("performance-quarter-zip", updated.Slug);
        var activity = await db.AdminActivityLogs.SingleAsync(value => value.Action == "store.product.slug-regenerated");
        Assert.Equal(product.Id, activity.TargetId);
        Assert.Contains("team-hoodie-copy-2", activity.Summary);
        Assert.Contains("performance-quarter-zip", activity.Summary);
    }

    [Theory]
    [InlineData(StoreProductStatus.Published, "team-hoodie-copy")]
    [InlineData(StoreProductStatus.Archived, "team-hoodie-copy-3")]
    [InlineData(StoreProductStatus.Draft, "team-hoodie")]
    public async Task RegenerateProductSlug_RejectsPublishedArchivedAndOrdinaryProducts(
        StoreProductStatus status,
        string slug)
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var product = new Product { Name = "Updated hoodie", Slug = slug, Status = status };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            Service(db).RegenerateProductSlugAsync(product.Id, actor.Id, CancellationToken.None));

        Assert.Contains("Slug", exception.Errors);
        Assert.Equal(slug, (await db.Products.AsNoTracking().SingleAsync(value => value.Id == product.Id)).Slug);
    }

    [Fact]
    public async Task InventoryAdjustments_AreAppendOnlyAndCannotUndercutReservations()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var product = new Product { Name = "Tee", Slug = "tee" };
        var variant = new ProductVariant
        {
            Product = product, Name = "Medium", Sku = "TEE-M",
            OnHandQuantity = 5, ReservedQuantity = 2, LowStockThreshold = 2
        };
        db.Products.Add(product);
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();
        var service = Service(db);

        var updated = await service.AdjustInventoryAsync(
            variant.Id,
            new InventoryAdjustmentWriteDto
            {
                QuantityDelta = -2,
                Reason = InventoryAdjustmentReason.Correction,
                RowVersion = Convert.ToBase64String(variant.RowVersion)
            },
            actor.Id,
            CancellationToken.None);

        Assert.Equal(3, updated.OnHandQuantity);
        var adjustment = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal(-2, adjustment.QuantityDelta);
        Assert.Equal(3, adjustment.ResultingOnHandQuantity);
        await Assert.ThrowsAsync<CmsConflictException>(() => service.AdjustInventoryAsync(
            variant.Id,
            new InventoryAdjustmentWriteDto
            {
                QuantityDelta = -2,
                Reason = InventoryAdjustmentReason.Correction,
                RowVersion = Convert.ToBase64String(variant.RowVersion)
            },
            actor.Id,
            CancellationToken.None));
        Assert.Single(await db.InventoryAdjustments.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Stocktake_RecordsExpectedCountAndOnlyCreatesChangedAdjustments()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var product = new Product { Name = "Hoodie", Slug = "hoodie" };
        var first = new ProductVariant { Product = product, Name = "Small", Sku = "H-S", OnHandQuantity = 4 };
        var second = new ProductVariant { Product = product, Name = "Large", Sku = "H-L", OnHandQuantity = 8 };
        db.Products.Add(product);
        db.ProductVariants.AddRange(first, second);
        await db.SaveChangesAsync();
        var service = Service(db);

        var result = await service.CompleteStocktakeAsync(
            new InventoryStocktakeWriteDto
            {
                Note = "Sunday shelf count",
                Lines = [
                    new(first.Id, 4, Convert.ToBase64String(first.RowVersion)),
                    new(second.Id, 6, Convert.ToBase64String(second.RowVersion))
                ]
            },
            actor.Id,
            CancellationToken.None);

        Assert.Equal(2, result.VariantCount);
        Assert.Equal(1, result.ChangedVariantCount);
        Assert.Equal(2, await db.InventoryStocktakeLines.CountAsync());
        var adjustment = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal(InventoryAdjustmentReason.Correction, adjustment.Reason);
        Assert.Equal(-2, adjustment.QuantityDelta);
    }

    [Fact]
    public async Task CreateProduct_RejectsVisualizerConditionsOutsideTheProductConfiguration()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var request = ProductRequest(optionId, valueId);
        request = new StoreProductWriteDto
        {
            Name = request.Name,
            BasePriceMinor = request.BasePriceMinor,
            Status = request.Status,
            Options = request.Options,
            Variants = request.Variants,
            VisualizerLayers = [new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                25,
                25,
                50,
                50,
                1,
                "normal")]
        };

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            Service(db).CreateProductAsync(request, actor.Id, CancellationToken.None));

        Assert.Contains("VisualizerLayers", exception.Errors);
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task CreateProduct_RejectsActiveUntrackedOptionsAndDirectsStaffToCustomizations()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var valid = ProductRequest(optionId, valueId);
        var request = new StoreProductWriteDto
        {
            Name = valid.Name,
            BasePriceMinor = valid.BasePriceMinor,
            Status = StoreProductStatus.Draft,
            Options = [new(optionId, "Logo Color", false, 0, true,
                [new(valueId, "Red", "#dc2626", null, 0, true)])],
            Variants = valid.Variants
        };

        var exception = await Assert.ThrowsAsync<CmsRequestValidationException>(() =>
            Service(db).CreateProductAsync(request, actor.Id, CancellationToken.None));

        Assert.Contains("Options", exception.Errors);
        Assert.Contains("Customizations", exception.Errors["Options"].Single());
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task UpdateProduct_ReplacesInventoryDimensionsWithoutDeletingVariantHistory()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var product = new Product
        {
            Name = "Team hoodie", Slug = "team-hoodie", BasePriceMinor = 5000,
            Status = StoreProductStatus.Draft
        };
        var size = new ProductOption { Product = product, Name = "Size", IsTracked = true, DisplayOrder = 0 };
        var color = new ProductOption { Product = product, Name = "Garment Color", IsTracked = true, DisplayOrder = 1 };
        var logo = new ProductOption { Product = product, Name = "Logo Color", IsTracked = true, DisplayOrder = 2 };
        var small = new ProductOptionValue { ProductOption = size, Name = "Small", Slug = "small", DisplayOrder = 0 };
        var red = new ProductOptionValue { ProductOption = color, Name = "Red", Slug = "red", DisplayOrder = 0 };
        var whiteLogo = new ProductOptionValue { ProductOption = logo, Name = "White", Slug = "white", DisplayOrder = 0 };
        size.Values.Add(small);
        color.Values.Add(red);
        logo.Values.Add(whiteLogo);
        product.Options.Add(size);
        product.Options.Add(color);
        product.Options.Add(logo);
        var historical = new ProductVariant
        {
            Product = product, Name = "Small / Red / White", Sku = "HOOD-OLD",
            OnHandQuantity = 4, ReservedQuantity = 1, LowStockThreshold = 2
        };
        historical.OptionValues.Add(new ProductVariantOptionValue { ProductVariant = historical, ProductOptionValue = small });
        historical.OptionValues.Add(new ProductVariantOptionValue { ProductVariant = historical, ProductOptionValue = red });
        historical.OptionValues.Add(new ProductVariantOptionValue { ProductVariant = historical, ProductOptionValue = whiteLogo });
        product.Variants.Add(historical);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var replacementId = Guid.NewGuid();
        var request = new StoreProductWriteDto
        {
            Name = product.Name,
            BasePriceMinor = product.BasePriceMinor,
            Status = StoreProductStatus.Draft,
            Options =
            [
                new(size.Id, "Size", true, 0, true, [new(small.Id, "Small", null, null, 0, true)]),
                new(color.Id, "Garment Color", true, 1, true, [new(red.Id, "Red", null, null, 0, true)])
            ],
            Variants = [new(replacementId, "Small / Red", "HOOD-S-RED", null, 2, true, null, [small.Id, red.Id])],
            ModifierGroups =
            [
                new(Guid.NewGuid(), "Logo Color", ProductModifierType.Color, true, 1, 1, 0, true,
                [
                    new(Guid.NewGuid(), "Red", 0, null, null, 0, true),
                    new(Guid.NewGuid(), "White", 0, null, null, 1, true),
                    new(Guid.NewGuid(), "Black", 0, null, null, 2, true)
                ])
            ]
        };

        await Service(db).UpdateProductAsync(product.Id, request, actor.Id, CancellationToken.None);

        db.ChangeTracker.Clear();
        var saved = await db.Products
            .Include(value => value.Options).ThenInclude(value => value.Values)
            .Include(value => value.Variants)
            .Include(value => value.ModifierGroups).ThenInclude(value => value.Values)
            .SingleAsync(value => value.Id == product.Id);
        var oldVariant = saved.Variants.Single(value => value.Id == historical.Id);
        var replacement = saved.Variants.Single(value => value.Id == replacementId);
        Assert.False(oldVariant.IsActive);
        Assert.Equal(4, oldVariant.OnHandQuantity);
        Assert.Equal(1, oldVariant.ReservedQuantity);
        Assert.True(replacement.IsActive);
        Assert.Equal(0, replacement.OnHandQuantity);
        Assert.Equal(0, replacement.ReservedQuantity);
        Assert.False(saved.Options.Single(value => value.Id == logo.Id).IsActive);
        var logoColors = saved.ModifierGroups.Single(value => value.Name == "Logo Color");
        Assert.True(logoColors.IsRequired);
        Assert.Equal(["Red", "White", "Black"], logoColors.Values.OrderBy(value => value.DisplayOrder).Select(value => value.Name));
    }

    private static StoreProductWriteDto ProductRequest(Guid optionId, Guid valueId) => new()
    {
        Name = "Team hoodie",
        BasePriceMinor = 5000,
        Status = StoreProductStatus.Draft,
        IsFeatured = true,
        Options = [new(optionId, "Size", true, 0, true, [new(valueId, "Medium", null, null, 0, true)])],
        Variants = [new(Guid.NewGuid(), "Medium", "HOOD-M", null, 3, true, null, [valueId])]
    };

    private static El1teDbContext Context()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseInMemoryDatabase($"store-admin-{Guid.NewGuid():N}")
            .Options;
        return new El1teDbContext(options);
    }

    private static async Task<User> AddActor(El1teDbContext db)
    {
        var actor = new User
        {
            FirstName = "Store", LastName = "Admin", Email = $"admin-{Guid.NewGuid():N}@example.invalid",
            PasswordHash = "not-used", Role = UserRole.SuperAdmin
        };
        db.Users.Add(actor);
        await db.SaveChangesAsync();
        return actor;
    }

    private static StoreAdminService Service(El1teDbContext db) =>
        new(db, new SlugGenerator(), new TestClock());

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
    }

}
