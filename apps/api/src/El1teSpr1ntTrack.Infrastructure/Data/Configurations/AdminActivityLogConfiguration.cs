using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class AdminActivityLogConfiguration : IEntityTypeConfiguration<AdminActivityLog>
{
    public void Configure(EntityTypeBuilder<AdminActivityLog> builder)
    {
        builder.Property(value => value.Action).HasMaxLength(80).IsRequired();
        builder.Property(value => value.TargetType).HasMaxLength(80).IsRequired();
        builder.Property(value => value.Summary).HasMaxLength(500).IsRequired();
        builder.Property(value => value.CorrelationId).HasMaxLength(100);
        builder.HasIndex(value => value.CreatedAt);
        builder.HasIndex(value => value.ActorUserId);

        builder.HasOne(value => value.ActorUser)
            .WithMany()
            .HasForeignKey(value => value.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
