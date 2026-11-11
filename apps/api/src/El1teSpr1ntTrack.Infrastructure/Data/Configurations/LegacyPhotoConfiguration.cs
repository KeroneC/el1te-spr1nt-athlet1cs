using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class LegacyPhotoConfiguration : IEntityTypeConfiguration<LegacyPhoto>,
    IEntityTypeConfiguration<LegacyPhotoSource>, IEntityTypeConfiguration<LegacyPhotoPage>
{
    public void Configure(EntityTypeBuilder<LegacyPhoto> builder)
    {
        builder.Property(x => x.Sha256).HasMaxLength(64).IsUnicode(false);
        builder.HasIndex(x => x.Sha256).IsUnique();
        builder.HasIndex(x => new { x.Review, x.Id });
        builder.Property(x => x.ReviewedAtUtc).IsConcurrencyToken();
        builder.HasMany(x => x.Sources).WithOne().HasForeignKey(x => x.LegacyPhotoId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<LegacyPhotoSource> builder)
    {
        builder.HasKey(x => x.UrlSha256);
        builder.Property(x => x.UrlSha256).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.SourceUrl).HasMaxLength(2048);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255);
        builder.HasMany(x => x.Pages).WithOne().HasForeignKey(x => x.SourceUrlSha256).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<LegacyPhotoPage> builder)
    {
        builder.HasKey(x => new { x.SourceUrlSha256, x.PageUrlSha256 });
        builder.Property(x => x.SourceUrlSha256).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.PageUrlSha256).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.PageUrl).HasMaxLength(2048);
        builder.Property(x => x.Collection).HasMaxLength(450);
        builder.HasIndex(x => x.Collection);
    }
}
