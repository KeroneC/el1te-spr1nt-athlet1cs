using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class HallOfFameInducteeConfiguration : IEntityTypeConfiguration<HallOfFameInductee>
{
    public void Configure(EntityTypeBuilder<HallOfFameInductee> builder)
    {
        builder.ConfigureCmsEntity("HallOfFameInductees");
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Slug).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Affiliation).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.PhotoUrl).HasMaxLength(500);
        builder.Property(item => item.PhotoAlt).HasMaxLength(500);
        builder.Property(item => item.DisplayOrder).HasDefaultValue(0);
        builder.Property(item => item.IsActive).HasDefaultValue(true);
        builder.HasIndex(item => item.Slug).IsUnique();
        builder.HasIndex(item => item.DisplayOrder);
        builder.HasIndex(item => item.InductionYear);
        builder.HasData(CmsSeedData.HallOfFameInductees);
    }
}
