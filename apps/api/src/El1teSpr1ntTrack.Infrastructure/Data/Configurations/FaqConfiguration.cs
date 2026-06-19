using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ConfigureCmsEntity("Faqs");
        builder.Property(faq => faq.Question).HasMaxLength(500).IsRequired();
        builder.Property(faq => faq.Answer).IsRequired();
        builder.Property(faq => faq.Category).HasMaxLength(100).IsRequired();
        builder.Property(faq => faq.DisplayOrder).HasDefaultValue(0);
        builder.Property(faq => faq.IsActive).HasDefaultValue(true);
        builder.HasIndex(faq => faq.DisplayOrder);
        builder.HasData(CmsSeedData.Faqs);
    }
}
