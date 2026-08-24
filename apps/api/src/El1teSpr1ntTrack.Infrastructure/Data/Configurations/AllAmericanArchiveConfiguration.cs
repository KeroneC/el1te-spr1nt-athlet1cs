using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class AllAmericanYearConfiguration : IEntityTypeConfiguration<AllAmericanYear>
{
    public void Configure(EntityTypeBuilder<AllAmericanYear> builder)
    {
        builder.ConfigureCmsEntity("AllAmericanYears");
        builder.Property(x => x.Slug).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => x.Year).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
        builder.HasOne(x => x.HeroMediaAsset).WithMany(x => x.HeroForAllAmericanYears)
            .HasForeignKey(x => x.HeroMediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AllAmericanYearMediaConfiguration : IEntityTypeConfiguration<AllAmericanYearMedia>
{
    public void Configure(EntityTypeBuilder<AllAmericanYearMedia> builder)
    {
        builder.ConfigureCmsEntity("AllAmericanYearMedia");
        builder.Property(x => x.AltTextOverride).HasMaxLength(500);
        builder.Property(x => x.CaptionOverride).HasMaxLength(1000);
        builder.HasIndex(x => new { x.AllAmericanYearId, x.MediaAssetId }).IsUnique();
        builder.HasIndex(x => new { x.AllAmericanYearId, x.DisplayOrder });
        builder.HasOne(x => x.AllAmericanYear).WithMany(x => x.Media).HasForeignKey(x => x.AllAmericanYearId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.MediaAsset).WithMany(x => x.AllAmericanYearMedia).HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AllAmericanRecipientConfiguration : IEntityTypeConfiguration<AllAmericanRecipient>
{
    public void Configure(EntityTypeBuilder<AllAmericanRecipient> builder)
    {
        builder.ConfigureCmsEntity("AllAmericanRecipients");
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.AllAmericanYearId, x.DisplayOrder });
        builder.HasOne(x => x.AllAmericanYear).WithMany(x => x.Recipients).HasForeignKey(x => x.AllAmericanYearId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.PhotoMediaAsset).WithMany(x => x.AllAmericanRecipientPhotos).HasForeignKey(x => x.PhotoMediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AllAmericanPerformanceConfiguration : IEntityTypeConfiguration<AllAmericanPerformance>
{
    public void Configure(EntityTypeBuilder<AllAmericanPerformance> builder)
    {
        builder.ConfigureCmsEntity("AllAmericanPerformances");
        builder.Property(x => x.EventName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Division).HasMaxLength(200);
        builder.HasIndex(x => new { x.AllAmericanYearId, x.DisplayOrder });
        builder.HasOne(x => x.AllAmericanYear).WithMany(x => x.Performances).HasForeignKey(x => x.AllAmericanYearId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AllAmericanPerformanceRecipientConfiguration : IEntityTypeConfiguration<AllAmericanPerformanceRecipient>
{
    public void Configure(EntityTypeBuilder<AllAmericanPerformanceRecipient> builder)
    {
        builder.ConfigureCmsEntity("AllAmericanPerformanceRecipients");
        builder.HasIndex(x => new { x.AllAmericanPerformanceId, x.AllAmericanRecipientId }).IsUnique();
        builder.HasOne(x => x.Performance).WithMany(x => x.Recipients).HasForeignKey(x => x.AllAmericanPerformanceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Recipient).WithMany(x => x.PerformanceRecipients).HasForeignKey(x => x.AllAmericanRecipientId).OnDelete(DeleteBehavior.Restrict);
    }
}
