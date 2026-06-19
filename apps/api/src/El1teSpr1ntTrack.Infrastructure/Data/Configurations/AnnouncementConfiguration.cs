using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ConfigureCmsEntity("Announcements");
        builder.Property(announcement => announcement.Title).HasMaxLength(200).IsRequired();
        builder.Property(announcement => announcement.Slug).HasMaxLength(200).IsRequired();
        builder.Property(announcement => announcement.Summary).HasMaxLength(500).IsRequired();
        builder.Property(announcement => announcement.Body).IsRequired();
        builder.Property(announcement => announcement.ImageUrl).HasMaxLength(500);
        builder.Property(announcement => announcement.IsFeatured).HasDefaultValue(false);
        builder.Property(announcement => announcement.IsPublished).HasDefaultValue(false);
        builder.HasIndex(announcement => announcement.Slug).IsUnique();
        builder.HasIndex(announcement => announcement.IsPublished);
        builder.HasIndex(announcement => announcement.PublishDateUtc);
        builder.HasData(CmsSeedData.Announcements);
    }
}
