using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace El1teSpr1ntTrack.IntegrationTests;

public sealed class CmsModelTests
{
    private readonly El1teDbContext _dbContext = CreateContext();

    [Theory]
    [InlineData(typeof(ContentBlock), nameof(ContentBlock.Key))]
    [InlineData(typeof(Announcement), nameof(Announcement.Slug))]
    [InlineData(typeof(Event), nameof(Event.Slug))]
    [InlineData(typeof(Sponsor), nameof(Sponsor.Slug))]
    public void CmsModel_DefinesRequiredUniqueIndexes(Type entityType, string propertyName)
    {
        var modelType = _dbContext.Model.FindEntityType(entityType);

        var index = Assert.Single(
            modelType!.GetIndexes(),
            candidate => candidate.Properties.Count == 1 && candidate.Properties[0].Name == propertyName);
        Assert.True(index.IsUnique);
    }

    [Theory]
    [InlineData(typeof(Event), nameof(Event.EventType))]
    [InlineData(typeof(Sponsor), nameof(Sponsor.Tier))]
    [InlineData(typeof(ContactSubmission), nameof(ContactSubmission.InquiryType))]
    [InlineData(typeof(ContactSubmission), nameof(ContactSubmission.Status))]
    public void CmsModel_StoresEnumsAsStrings(Type entityType, string propertyName)
    {
        var property = _dbContext.Model.FindEntityType(entityType)!.FindProperty(propertyName);

        Assert.Equal(typeof(string), property!.GetProviderClrType());
    }

    [Theory]
    [InlineData(typeof(SiteSetting), 1)]
    [InlineData(typeof(ContentBlock), 7)]
    [InlineData(typeof(Announcement), 3)]
    [InlineData(typeof(Event), 4)]
    [InlineData(typeof(Coach), 3)]
    [InlineData(typeof(Sponsor), 4)]
    [InlineData(typeof(Faq), 6)]
    public void CmsModel_ContainsRequiredSeedData(Type entityType, int expectedCount)
    {
        var designTimeModel = _dbContext.GetService<IDesignTimeModel>().Model;
        var seedData = designTimeModel.FindEntityType(entityType)!.GetSeedData();

        Assert.Equal(expectedCount, seedData.Count());
    }

    [Fact]
    public void CommerceModel_PreservesOrderHistoryWhenProductsAreArchived()
    {
        var orderItemType = _dbContext.Model.FindEntityType(typeof(OrderItem))!;
        var productRelationship = Assert.Single(
            orderItemType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Product));

        Assert.Equal(DeleteBehavior.NoAction, productRelationship.DeleteBehavior);
    }

    [Theory]
    [InlineData(typeof(Product), nameof(Product.SquareCatalogObjectId))]
    [InlineData(typeof(ProductVariant), nameof(ProductVariant.SquareCatalogObjectId))]
    [InlineData(typeof(ProductCategory), nameof(ProductCategory.SquareCatalogObjectId))]
    public void CommerceCatalogModel_ProtectsSquareImportIdempotency(Type entityType, string propertyName)
    {
        var modelType = _dbContext.Model.FindEntityType(entityType)!;
        var index = Assert.Single(
            modelType.GetIndexes(),
            candidate => candidate.Properties.Count == 1 && candidate.Properties[0].Name == propertyName);
        Assert.True(index.IsUnique);
        Assert.NotNull(index.GetFilter());
    }

    [Fact]
    public void InventoryStocktakeModel_PreservesAppendOnlyHistory()
    {
        var line = _dbContext.Model.FindEntityType(typeof(InventoryStocktakeLine))!;
        var adjustmentRelationship = Assert.Single(
            line.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(InventoryAdjustment));
        var variantRelationship = Assert.Single(
            line.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(ProductVariant));

        Assert.Equal(DeleteBehavior.NoAction, adjustmentRelationship.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, variantRelationship.DeleteBehavior);
    }

    private static El1teDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<El1teDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=El1teSpr1ntTrack_ModelTests;Trusted_Connection=True")
            .Options;

        return new El1teDbContext(options);
    }
}
