using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(value => value.Name).HasMaxLength(200).IsRequired();
        builder.Property(value => value.Slug).HasMaxLength(220).IsRequired();
        builder.Property(value => value.ShortDescription).HasMaxLength(500);
        builder.Property(value => value.Description).HasMaxLength(5000);
        builder.Property(value => value.Currency).HasMaxLength(3).IsRequired();
        builder.Property(value => value.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.SquareCatalogObjectId).HasMaxLength(100);
        builder.HasIndex(value => value.Slug).IsUnique();
        builder.HasIndex(value => value.SquareCatalogObjectId)
            .IsUnique()
            .HasFilter("[SquareCatalogObjectId] IS NOT NULL");
        builder.HasIndex(value => new { value.Status, value.DisplayOrder });
        builder.HasOne(value => value.Category)
            .WithMany(value => value.Products)
            .HasForeignKey(value => value.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.Property(value => value.Name).HasMaxLength(120).IsRequired();
        builder.Property(value => value.Slug).HasMaxLength(140).IsRequired();
        builder.Property(value => value.SquareCatalogObjectId).HasMaxLength(100);
        builder.HasIndex(value => value.Slug).IsUnique();
        builder.HasIndex(value => value.SquareCatalogObjectId)
            .IsUnique()
            .HasFilter("[SquareCatalogObjectId] IS NOT NULL");
    }
}

public sealed class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.Property(value => value.Role).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.AltTextOverride).HasMaxLength(500);
        builder.HasIndex(value => new { value.ProductId, value.Role, value.DisplayOrder });
        builder.HasOne(value => value.Product).WithMany(value => value.Media)
            .HasForeignKey(value => value.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.MediaAsset).WithMany()
            .HasForeignKey(value => value.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.Property(value => value.Name).HasMaxLength(100).IsRequired();
        builder.Property(value => value.SquareCatalogObjectId).HasMaxLength(100);
        builder.HasIndex(value => new { value.ProductId, value.Name }).IsUnique();
        builder.HasOne(value => value.Product).WithMany(value => value.Options)
            .HasForeignKey(value => value.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.Property(value => value.Name).HasMaxLength(100).IsRequired();
        builder.Property(value => value.Slug).HasMaxLength(120).IsRequired();
        builder.Property(value => value.ColorHex).HasMaxLength(9);
        builder.Property(value => value.SquareCatalogObjectId).HasMaxLength(100);
        builder.HasIndex(value => new { value.ProductOptionId, value.Slug }).IsUnique();
        builder.HasOne(value => value.ProductOption).WithMany(value => value.Values)
            .HasForeignKey(value => value.ProductOptionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.SwatchMediaAsset).WithMany()
            .HasForeignKey(value => value.SwatchMediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ProductVariants_Inventory",
                "[OnHandQuantity] >= 0 AND [ReservedQuantity] >= 0 AND [ReservedQuantity] <= [OnHandQuantity]");
            table.HasCheckConstraint(
                "CK_ProductVariants_LowStockThreshold",
                "[LowStockThreshold] >= 0");
        });
        builder.Property(value => value.Name).HasMaxLength(240).IsRequired();
        builder.Property(value => value.Sku).HasMaxLength(100).IsRequired();
        builder.Property(value => value.SquareCatalogObjectId).HasMaxLength(100);
        builder.Property(value => value.RowVersion).IsRowVersion();
        builder.HasIndex(value => value.Sku).IsUnique();
        builder.HasIndex(value => value.SquareCatalogObjectId)
            .IsUnique()
            .HasFilter("[SquareCatalogObjectId] IS NOT NULL");
        builder.HasOne(value => value.Product).WithMany(value => value.Variants)
            .HasForeignKey(value => value.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.HasKey(value => new { value.ProductVariantId, value.ProductOptionValueId });
        builder.HasOne(value => value.ProductVariant).WithMany(value => value.OptionValues)
            .HasForeignKey(value => value.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.ProductOptionValue).WithMany(value => value.VariantValues)
            .HasForeignKey(value => value.ProductOptionValueId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ProductModifierGroups_Selections",
            "[MinimumSelections] >= 0 AND [MaximumSelections] >= [MinimumSelections]"));
        builder.Property(value => value.Name).HasMaxLength(120).IsRequired();
        builder.Property(value => value.Type).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(value => value.Product).WithMany(value => value.ModifierGroups)
            .HasForeignKey(value => value.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductModifierValueConfiguration : IEntityTypeConfiguration<ProductModifierValue>
{
    public void Configure(EntityTypeBuilder<ProductModifierValue> builder)
    {
        builder.Property(value => value.Name).HasMaxLength(120).IsRequired();
        builder.Property(value => value.ColorHex).HasMaxLength(9);
        builder.HasOne(value => value.ProductModifierGroup).WithMany(value => value.Values)
            .HasForeignKey(value => value.ProductModifierGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.OverlayMediaAsset).WithMany()
            .HasForeignKey(value => value.OverlayMediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductVisualizerLayerConfiguration : IEntityTypeConfiguration<ProductVisualizerLayer>
{
    public void Configure(EntityTypeBuilder<ProductVisualizerLayer> builder)
    {
        builder.Property(value => value.XPercent).HasPrecision(5, 2);
        builder.Property(value => value.YPercent).HasPrecision(5, 2);
        builder.Property(value => value.WidthPercent).HasPrecision(5, 2);
        builder.Property(value => value.HeightPercent).HasPrecision(5, 2);
        builder.Property(value => value.BlendMode).HasMaxLength(30);
        builder.HasOne(value => value.Product).WithMany(value => value.VisualizerLayers)
            .HasForeignKey(value => value.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.MediaAsset).WithMany()
            .HasForeignKey(value => value.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.ProductOptionValue).WithMany()
            .HasForeignKey(value => value.ProductOptionValueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.ProductModifierValue).WithMany()
            .HasForeignKey(value => value.ProductModifierValueId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(value => value.PublicNumber).HasMaxLength(30).IsRequired();
        builder.Property(value => value.CheckoutAttemptId).HasMaxLength(64).IsRequired();
        builder.Property(value => value.CheckoutPayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(value => value.CheckoutReturnTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(value => value.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(value => value.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(value => value.CustomerPhone).HasMaxLength(40).IsRequired();
        builder.Property(value => value.AthleteTeamNote).HasMaxLength(300);
        builder.Property(value => value.FulfillmentNote).HasMaxLength(1000);
        builder.Property(value => value.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(value => value.PaymentProvider).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.PaymentStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Currency).HasMaxLength(3).IsRequired();
        builder.Property(value => value.TrackingTokenHash).HasMaxLength(64);
        builder.Property(value => value.SquareOrderId).HasMaxLength(100);
        builder.Property(value => value.SquarePaymentId).HasMaxLength(100);
        builder.Property(value => value.SquarePaymentLinkId).HasMaxLength(100);
        builder.Property(value => value.SquarePaymentLinkUrl).HasMaxLength(500);
        builder.HasIndex(value => value.PublicNumber).IsUnique();
        builder.HasIndex(value => value.CheckoutAttemptId).IsUnique();
        builder.HasIndex(value => value.CheckoutReturnTokenHash).IsUnique();
        builder.HasIndex(value => value.TrackingTokenHash).IsUnique().HasFilter("[TrackingTokenHash] IS NOT NULL");
        builder.HasIndex(value => value.SquareOrderId).IsUnique().HasFilter("[SquareOrderId] IS NOT NULL");
        builder.HasIndex(value => new { value.Status, value.CreatedAt });
        builder.HasMany(value => value.OrderItems).WithOne(value => value.Order)
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(value => value.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(value => value.VariantName).HasMaxLength(240).IsRequired();
        builder.Property(value => value.Sku).HasMaxLength(100).IsRequired();
        builder.Property(value => value.ConfigurationJson).HasMaxLength(8000).IsRequired();
        builder.HasOne(value => value.Product).WithMany(value => value.OrderItems)
            .HasForeignKey(value => value.ProductId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(value => value.ProductVariant).WithMany(value => value.OrderItems)
            .HasForeignKey(value => value.ProductVariantId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.Property(value => value.Reason).HasConversion<string>().HasMaxLength(40);
        builder.Property(value => value.Note).HasMaxLength(1000);
        builder.HasIndex(value => new { value.ProductVariantId, value.CreatedAt });
        builder.HasOne(value => value.ProductVariant).WithMany(value => value.InventoryAdjustments)
            .HasForeignKey(value => value.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Order).WithMany()
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(value => value.ActorUser).WithMany()
            .HasForeignKey(value => value.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryStocktakeConfiguration : IEntityTypeConfiguration<InventoryStocktake>
{
    public void Configure(EntityTypeBuilder<InventoryStocktake> builder)
    {
        builder.Property(value => value.Note).HasMaxLength(1000);
        builder.HasIndex(value => value.CreatedAt);
        builder.HasOne(value => value.ActorUser).WithMany()
            .HasForeignKey(value => value.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(value => value.Lines).WithOne(value => value.InventoryStocktake)
            .HasForeignKey(value => value.InventoryStocktakeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InventoryStocktakeLineConfiguration : IEntityTypeConfiguration<InventoryStocktakeLine>
{
    public void Configure(EntityTypeBuilder<InventoryStocktakeLine> builder)
    {
        builder.HasIndex(value => new { value.InventoryStocktakeId, value.ProductVariantId }).IsUnique();
        builder.HasOne(value => value.ProductVariant).WithMany()
            .HasForeignKey(value => value.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.InventoryAdjustment).WithMany()
            .HasForeignKey(value => value.InventoryAdjustmentId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class SquareCatalogImportRunConfiguration : IEntityTypeConfiguration<SquareCatalogImportRun>
{
    public void Configure(EntityTypeBuilder<SquareCatalogImportRun> builder)
    {
        builder.Property(value => value.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.SafeFailureCode).HasMaxLength(100);
        builder.HasIndex(value => value.CreatedAt);
        builder.HasOne(value => value.ActorUser).WithMany()
            .HasForeignKey(value => value.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasIndex(value => value.ExpiresAtUtc);
        builder.HasOne(value => value.Order).WithMany(value => value.Reservations)
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InventoryReservationItemConfiguration : IEntityTypeConfiguration<InventoryReservationItem>
{
    public void Configure(EntityTypeBuilder<InventoryReservationItem> builder)
    {
        builder.HasOne(value => value.InventoryReservation).WithMany(value => value.Items)
            .HasForeignKey(value => value.InventoryReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.ProductVariant).WithMany(value => value.ReservationItems)
            .HasForeignKey(value => value.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.Property(value => value.FromStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(value => value.ToStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(value => value.Note).HasMaxLength(1000);
        builder.HasIndex(value => new { value.OrderId, value.CreatedAt });
        builder.HasOne(value => value.Order).WithMany(value => value.StatusHistory)
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.ActorUser).WithMany()
            .HasForeignKey(value => value.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CommerceRefundConfiguration : IEntityTypeConfiguration<CommerceRefund>
{
    public void Configure(EntityTypeBuilder<CommerceRefund> builder)
    {
        builder.Property(value => value.Currency).HasMaxLength(3).IsRequired();
        builder.Property(value => value.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(value => value.SquareRefundId).HasMaxLength(100);
        builder.Property(value => value.SafeFailureCode).HasMaxLength(100);
        builder.HasIndex(value => value.SquareRefundId).IsUnique().HasFilter("[SquareRefundId] IS NOT NULL");
        builder.HasOne(value => value.Order).WithMany(value => value.Refunds)
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.ActorUser).WithMany()
            .HasForeignKey(value => value.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CommerceRefundLineConfiguration : IEntityTypeConfiguration<CommerceRefundLine>
{
    public void Configure(EntityTypeBuilder<CommerceRefundLine> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_CommerceRefundLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint(
                "CK_CommerceRefundLines_RestockQuantity",
                "[RestockQuantity] >= 0 AND [RestockQuantity] <= [Quantity]");
        });
        builder.HasIndex(value => new { value.CommerceRefundId, value.OrderItemId }).IsUnique();
        builder.HasOne(value => value.CommerceRefund).WithMany(value => value.Lines)
            .HasForeignKey(value => value.CommerceRefundId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.OrderItem).WithMany()
            .HasForeignKey(value => value.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.InventoryAdjustment).WithMany()
            .HasForeignKey(value => value.InventoryAdjustmentId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class OrderInternalNoteConfiguration : IEntityTypeConfiguration<OrderInternalNote>
{
    public void Configure(EntityTypeBuilder<OrderInternalNote> builder)
    {
        builder.Property(value => value.Note).HasMaxLength(2000).IsRequired();
        builder.HasIndex(value => new { value.OrderId, value.CreatedAt });
        builder.HasOne(value => value.Order).WithMany(value => value.InternalNotes)
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.ActorUser).WithMany()
            .HasForeignKey(value => value.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CommerceEmailMessageConfiguration : IEntityTypeConfiguration<CommerceEmailMessage>
{
    public void Configure(EntityTypeBuilder<CommerceEmailMessage> builder)
    {
        builder.Property(value => value.TemplateName).HasMaxLength(100).IsRequired();
        builder.Property(value => value.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(value => value.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.ProviderMessageId).HasMaxLength(200);
        builder.Property(value => value.SafeFailureCode).HasMaxLength(100);
        builder.HasIndex(value => new { value.Status, value.CreatedAt });
        builder.HasIndex(value => value.ProviderMessageId)
            .IsUnique()
            .HasFilter("[ProviderMessageId] IS NOT NULL");
        builder.HasOne(value => value.Order).WithMany(value => value.EmailHistory)
            .HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SquareWebhookEventConfiguration : IEntityTypeConfiguration<SquareWebhookEvent>
{
    public void Configure(EntityTypeBuilder<SquareWebhookEvent> builder)
    {
        builder.Property(value => value.SquareEventId).HasMaxLength(100).IsRequired();
        builder.Property(value => value.EventType).HasMaxLength(100).IsRequired();
        builder.Property(value => value.MerchantId).HasMaxLength(100);
        builder.Property(value => value.ObjectId).HasMaxLength(100);
        builder.Property(value => value.PayloadSha256).HasMaxLength(64).IsRequired();
        builder.HasIndex(value => value.SquareEventId).IsUnique();
        builder.HasIndex(value => value.ProcessedAtUtc);
    }
}

public sealed class CommerceOutboxMessageConfiguration : IEntityTypeConfiguration<CommerceOutboxMessage>
{
    public void Configure(EntityTypeBuilder<CommerceOutboxMessage> builder)
    {
        builder.Property(value => value.MessageType).HasMaxLength(100).IsRequired();
        builder.Property(value => value.PayloadJson).HasMaxLength(4000).IsRequired();
        builder.Property(value => value.SafeLastError).HasMaxLength(200);
        builder.Property(value => value.RowVersion).IsRowVersion();
        builder.HasIndex(value => new { value.ProcessedAtUtc, value.AvailableAtUtc });
    }
}
