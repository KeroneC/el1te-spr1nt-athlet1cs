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
        entity.Variants.Single().OnHandQuantity = 6;
        await db.SaveChangesAsync();

        var duplicate = await service.DuplicateProductAsync(created.Id, actor.Id, CancellationToken.None);

        Assert.Equal(StoreProductStatus.Draft, duplicate.Status);
        Assert.False(duplicate.IsFeatured);
        Assert.Null(duplicate.SquareCatalogObjectId);
        var variant = Assert.Single(duplicate.Variants);
        Assert.Equal(0, variant.OnHandQuantity);
        Assert.Equal(0, variant.ReservedQuantity);
        Assert.EndsWith("-COPY", variant.Sku);
        Assert.NotEqual(created.Options.Single().Values.Single().Id, duplicate.Options.Single().Values.Single().Id);
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
    public async Task SquareImport_IsIdempotentAndLeavesProductsAsDrafts()
    {
        await using var db = Context();
        var actor = await AddActor(db);
        var square = new FakeSquareClient(new SquareCatalogSnapshot([
            new("ITEM-1", 4, "Square tee", "Description", "CAT-1", "Apparel", [],
                [new("OPTION-1", "Size", 0, [new("VALUE-1", "Small", null, 0)])],
                [new("VAR-1", 8, "Small", "SQ-TEE-S", 2500, "USD", 5, ["VALUE-1"])])
        ]));
        var service = Service(db, square);

        var first = await service.ImportSquareCatalogAsync(actor.Id, CancellationToken.None);
        var second = await service.ImportSquareCatalogAsync(actor.Id, CancellationToken.None);

        Assert.Equal(1, first.ProductsCreated);
        Assert.Equal(0, second.ProductsCreated);
        Assert.Equal(1, second.ProductsSkipped);
        var product = await db.Products.Include(value => value.Variants).SingleAsync();
        Assert.Equal(StoreProductStatus.Draft, product.Status);
        Assert.Equal("ITEM-1", product.SquareCatalogObjectId);
        Assert.Equal(5, product.Variants.Single().OnHandQuantity);
        Assert.Single(await db.InventoryAdjustments.ToListAsync());
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

    private static StoreAdminService Service(El1teDbContext db, ISquareClient? square = null) =>
        new(db, new SlugGenerator(), new TestClock(), square ?? new FakeSquareClient(new SquareCatalogSnapshot([])), new NullImageImporter());

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class NullImageImporter : ISquareCatalogImageImporter
    {
        public Task<Guid?> ImportAsync(SquareCatalogImage image, string productName, Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);
    }

    private sealed class FakeSquareClient(SquareCatalogSnapshot snapshot) : ISquareClient
    {
        public Task<SquareCatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken) => Task.FromResult(snapshot);
        public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<SquarePaymentLinkResult> CreatePaymentLinkAsync(SquarePaymentLinkCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SquarePaymentResult> RetrievePaymentAsync(string paymentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SquareRefundResult> RefundPaymentAsync(SquareRefundCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
