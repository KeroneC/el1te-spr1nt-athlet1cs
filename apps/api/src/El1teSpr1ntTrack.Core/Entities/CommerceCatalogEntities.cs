using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Core.Entities;

public sealed class ProductCategory : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SquareCatalogObjectId { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public sealed class ProductMedia : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;
    public ProductMediaRole Role { get; set; } = ProductMediaRole.Gallery;
    public string? AltTextOverride { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class ProductOption : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsTracked { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SquareCatalogObjectId { get; set; }
    public ICollection<ProductOptionValue> Values { get; set; } = new List<ProductOptionValue>();
}

public sealed class ProductOptionValue : EntityBase
{
    public Guid ProductOptionId { get; set; }
    public ProductOption ProductOption { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public Guid? SwatchMediaAssetId { get; set; }
    public MediaAsset? SwatchMediaAsset { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SquareCatalogObjectId { get; set; }
    public ICollection<ProductVariantOptionValue> VariantValues { get; set; } = new List<ProductVariantOptionValue>();
}

public sealed class ProductVariant : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public long? PriceOverrideMinor { get; set; }
    public int OnHandQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 3;
    public bool IsActive { get; set; } = true;
    public string? SquareCatalogObjectId { get; set; }
    public long? SquareCatalogVersion { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<ProductVariantOptionValue> OptionValues { get; set; } = new List<ProductVariantOptionValue>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = new List<InventoryAdjustment>();
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();
}

public sealed class ProductVariantOptionValue
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public Guid ProductOptionValueId { get; set; }
    public ProductOptionValue ProductOptionValue { get; set; } = null!;
}

public sealed class ProductModifierGroup : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ProductModifierType Type { get; set; } = ProductModifierType.Choice;
    public bool IsRequired { get; set; }
    public int MinimumSelections { get; set; }
    public int MaximumSelections { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ProductModifierValue> Values { get; set; } = new List<ProductModifierValue>();
}

public sealed class ProductModifierValue : EntityBase
{
    public Guid ProductModifierGroupId { get; set; }
    public ProductModifierGroup ProductModifierGroup { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public long PriceAdjustmentMinor { get; set; }
    public string? ColorHex { get; set; }
    public Guid? OverlayMediaAssetId { get; set; }
    public MediaAsset? OverlayMediaAsset { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductVisualizerLayer : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;
    public Guid? ProductOptionValueId { get; set; }
    public ProductOptionValue? ProductOptionValue { get; set; }
    public Guid? ProductModifierValueId { get; set; }
    public ProductModifierValue? ProductModifierValue { get; set; }
    public decimal XPercent { get; set; }
    public decimal YPercent { get; set; }
    public decimal WidthPercent { get; set; } = 100;
    public decimal HeightPercent { get; set; } = 100;
    public int ZIndex { get; set; }
    public string BlendMode { get; set; } = "normal";
}
