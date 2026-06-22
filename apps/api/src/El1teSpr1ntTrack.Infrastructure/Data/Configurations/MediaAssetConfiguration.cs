using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ConfigureCmsEntity("MediaAssets");
        builder.Property(asset => asset.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(asset => asset.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(asset => asset.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.FileExtension).HasMaxLength(10).IsRequired();
        builder.Property(asset => asset.Title).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.AltText).HasMaxLength(500).IsRequired();
        builder.Property(asset => asset.Caption).HasMaxLength(1000);
        builder.Property(asset => asset.PublicUrl).HasMaxLength(500).IsRequired();
        builder.Property(asset => asset.IsActive).HasDefaultValue(true);
        builder.HasIndex(asset => asset.StorageKey).IsUnique();
        builder.HasIndex(asset => asset.PublicUrl).IsUnique();
        builder.HasIndex(asset => asset.IsActive);
        builder.HasIndex(asset => asset.CreatedAtUtc);
        builder.HasOne(asset => asset.UploadedByUser)
            .WithMany(user => user.UploadedMedia)
            .HasForeignKey(asset => asset.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
