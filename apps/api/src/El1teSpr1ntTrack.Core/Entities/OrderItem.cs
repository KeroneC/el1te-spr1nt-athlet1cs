namespace El1teSpr1ntTrack.Core.Entities;

public sealed class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }

    public Order? Order { get; set; }

    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
