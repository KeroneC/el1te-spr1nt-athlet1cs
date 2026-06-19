using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ConfigureCmsEntity("Events");
        builder.Property(cmsEvent => cmsEvent.Title).HasMaxLength(200).IsRequired();
        builder.Property(cmsEvent => cmsEvent.Slug).HasMaxLength(200).IsRequired();
        builder.Property(cmsEvent => cmsEvent.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(EventType.Other)
            .IsRequired();
        builder.Property(cmsEvent => cmsEvent.LocationName).HasMaxLength(200).IsRequired();
        builder.Property(cmsEvent => cmsEvent.Address).HasMaxLength(300);
        builder.Property(cmsEvent => cmsEvent.Description).IsRequired();
        builder.Property(cmsEvent => cmsEvent.RegistrationUrl).HasMaxLength(500);
        builder.Property(cmsEvent => cmsEvent.ImageUrl).HasMaxLength(500);
        builder.Property(cmsEvent => cmsEvent.IsFeatured).HasDefaultValue(false);
        builder.Property(cmsEvent => cmsEvent.IsPublished).HasDefaultValue(false);
        builder.HasIndex(cmsEvent => cmsEvent.Slug).IsUnique();
        builder.HasIndex(cmsEvent => cmsEvent.EventType);
        builder.HasIndex(cmsEvent => cmsEvent.StartDateTimeUtc);
        builder.HasIndex(cmsEvent => cmsEvent.IsPublished);
        builder.HasData(CmsSeedData.Events);
    }
}
