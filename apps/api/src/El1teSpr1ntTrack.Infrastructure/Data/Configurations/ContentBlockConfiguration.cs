using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class ContentBlockConfiguration : IEntityTypeConfiguration<ContentBlock>
{
    public void Configure(EntityTypeBuilder<ContentBlock> builder)
    {
        builder.ConfigureCmsEntity("ContentBlocks");
        builder.Property(block => block.Key).HasMaxLength(150).IsRequired();
        builder.Property(block => block.Title).HasMaxLength(200).IsRequired();
        builder.Property(block => block.Summary).HasMaxLength(500);
        builder.Property(block => block.Body).IsRequired();
        builder.Property(block => block.ImageUrl).HasMaxLength(500);
        builder.Property(block => block.CtaText).HasMaxLength(100);
        builder.Property(block => block.CtaUrl).HasMaxLength(500);
        builder.Property(block => block.DisplayOrder).HasDefaultValue(0);
        builder.Property(block => block.IsPublished).HasDefaultValue(false);
        builder.HasIndex(block => block.Key).IsUnique();
        builder.HasData(CmsSeedData.ContentBlocks);
    }
}
