using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ConfigureCmsEntity("SiteSettings");
        builder.Property(setting => setting.ClubName).HasMaxLength(200).IsRequired();
        builder.Property(setting => setting.Slogan).HasMaxLength(300).IsRequired();
        builder.Property(setting => setting.ContactEmail).HasMaxLength(256).IsRequired();
        builder.Property(setting => setting.PhoneNumber).HasMaxLength(30);
        builder.Property(setting => setting.AddressLine1).HasMaxLength(200);
        builder.Property(setting => setting.AddressLine2).HasMaxLength(200);
        builder.Property(setting => setting.City).HasMaxLength(100);
        builder.Property(setting => setting.State).HasMaxLength(50);
        builder.Property(setting => setting.ZipCode).HasMaxLength(20);
        builder.Property(setting => setting.FacebookUrl).HasMaxLength(500);
        builder.Property(setting => setting.InstagramUrl).HasMaxLength(500);
        builder.Property(setting => setting.YouTubeUrl).HasMaxLength(500);
        builder.Property(setting => setting.PrimaryCtaText).HasMaxLength(100).IsRequired();
        builder.Property(setting => setting.PrimaryCtaUrl).HasMaxLength(500).IsRequired();
        builder.Property(setting => setting.SecondaryCtaText).HasMaxLength(100).IsRequired();
        builder.Property(setting => setting.SecondaryCtaUrl).HasMaxLength(500).IsRequired();
        builder.Property(setting => setting.LogoUrl).HasMaxLength(500);
        builder.HasData(CmsSeedData.SiteSettings);
    }
}
