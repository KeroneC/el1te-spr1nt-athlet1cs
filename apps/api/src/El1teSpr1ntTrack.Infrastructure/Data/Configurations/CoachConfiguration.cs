using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.ConfigureCmsEntity("Coaches");
        builder.Property(coach => coach.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(coach => coach.LastName).HasMaxLength(100).IsRequired();
        builder.Property(coach => coach.Role).HasMaxLength(150).IsRequired();
        builder.Property(coach => coach.Bio).IsRequired();
        builder.Property(coach => coach.ImageUrl).HasMaxLength(500);
        builder.Property(coach => coach.Email).HasMaxLength(256);
        builder.Property(coach => coach.IsEmailPublic).HasDefaultValue(false);
        builder.Property(coach => coach.DisplayOrder).HasDefaultValue(0);
        builder.Property(coach => coach.IsActive).HasDefaultValue(true);
        builder.HasIndex(coach => coach.DisplayOrder);
        builder.HasData(CmsSeedData.Coaches);
    }
}
