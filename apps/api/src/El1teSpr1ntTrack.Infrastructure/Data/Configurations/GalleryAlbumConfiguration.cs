using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class GalleryAlbumConfiguration : IEntityTypeConfiguration<GalleryAlbum>
{
    public void Configure(EntityTypeBuilder<GalleryAlbum> builder)
    {
        builder.ConfigureCmsEntity("GalleryAlbums");
        builder.Property(album => album.Title).HasMaxLength(200).IsRequired();
        builder.Property(album => album.Slug).HasMaxLength(200).IsRequired();
        builder.Property(album => album.Description).HasMaxLength(4000).IsRequired();
        builder.Property(album => album.IsPublished).HasDefaultValue(false);
        builder.HasIndex(album => album.Slug).IsUnique();
        builder.HasIndex(album => album.IsPublished);
        builder.HasIndex(album => album.DisplayOrder);
        builder.HasOne(album => album.CoverMediaAsset)
            .WithMany(asset => asset.CoverForAlbums)
            .HasForeignKey(album => album.CoverMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GalleryAlbumMediaConfiguration : IEntityTypeConfiguration<GalleryAlbumMedia>
{
    public void Configure(EntityTypeBuilder<GalleryAlbumMedia> builder)
    {
        builder.ConfigureCmsEntity("GalleryAlbumMedia");
        builder.Property(item => item.CaptionOverride).HasMaxLength(1000);
        builder.Property(item => item.AltTextOverride).HasMaxLength(500);
        builder.HasIndex(item => new { item.GalleryAlbumId, item.MediaAssetId }).IsUnique();
        builder.HasIndex(item => new { item.GalleryAlbumId, item.DisplayOrder });
        builder.HasOne(item => item.GalleryAlbum)
            .WithMany(album => album.Media)
            .HasForeignKey(item => item.GalleryAlbumId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.MediaAsset)
            .WithMany(asset => asset.AlbumMedia)
            .HasForeignKey(item => item.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
