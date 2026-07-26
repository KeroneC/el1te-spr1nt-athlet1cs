namespace El1teSpr1ntTrack.Core.Entities;

public sealed class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }

    public Order? Order { get; set; }

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public Guid? ProductVariantId { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string VariantName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string ConfigurationJson { get; set; } = "{}";

    public int Quantity { get; set; }

    public long UnitPriceMinor { get; set; }

    public long ModifierTotalMinor { get; set; }

    public long LineTotalMinor { get; set; }
}
