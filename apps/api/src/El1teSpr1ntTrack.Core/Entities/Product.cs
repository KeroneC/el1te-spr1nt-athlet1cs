using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class Product : EntityBase
{
    public Guid? CategoryId { get; set; }

    public ProductCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public long BasePriceMinor { get; set; }

    public string Currency { get; set; } = "USD";

    public StoreProductStatus Status { get; set; } = StoreProductStatus.Draft;

    public bool IsFeatured { get; set; }

    public int DisplayOrder { get; set; }

    public bool AllowsSpecialRequests { get; set; }

    public ProductFulfillmentMode FulfillmentMode { get; set; } = ProductFulfillmentMode.ClubHandoff;

    public string? PrintifyProductId { get; set; }

    public int? PrintifyBlueprintId { get; set; }

    public int? PrintifyProviderId { get; set; }

    public DateTimeOffset? PrintifyLastSyncedAtUtc { get; set; }

    public string? SquareCatalogObjectId { get; set; }

    public long? SquareCatalogVersion { get; set; }

    public DateTimeOffset? ImportedAtUtc { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();

    public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public ICollection<ProductModifierGroup> ModifierGroups { get; set; } = new List<ProductModifierGroup>();

    public ICollection<ProductVisualizerLayer> VisualizerLayers { get; set; } = new List<ProductVisualizerLayer>();
}
