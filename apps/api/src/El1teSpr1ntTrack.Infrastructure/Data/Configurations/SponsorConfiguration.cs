using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class SponsorConfiguration : IEntityTypeConfiguration<Sponsor>
{
    public void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        builder.ConfigureCmsEntity("Sponsors");
        builder.Property(sponsor => sponsor.Name).HasMaxLength(200).IsRequired();
        builder.Property(sponsor => sponsor.Slug).HasMaxLength(200).IsRequired();
        builder.Property(sponsor => sponsor.Tier).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(sponsor => sponsor.LogoUrl).HasMaxLength(500);
        builder.Property(sponsor => sponsor.WebsiteUrl).HasMaxLength(500);
        builder.Property(sponsor => sponsor.Description).HasMaxLength(2000);
        builder.Property(sponsor => sponsor.DisplayOrder).HasDefaultValue(0);
        builder.Property(sponsor => sponsor.IsActive).HasDefaultValue(true);
        builder.HasIndex(sponsor => sponsor.Slug).IsUnique();
        builder.HasData(CmsSeedData.Sponsors);
    }
}
