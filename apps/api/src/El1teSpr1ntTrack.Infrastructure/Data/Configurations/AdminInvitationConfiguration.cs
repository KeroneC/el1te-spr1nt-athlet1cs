using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class AdminInvitationConfiguration : IEntityTypeConfiguration<AdminInvitation>
{
    public void Configure(EntityTypeBuilder<AdminInvitation> builder)
    {
        builder.Property(value => value.Email).HasMaxLength(256).IsRequired();
        builder.Property(value => value.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(value => value.LastName).HasMaxLength(100).IsRequired();
        builder.Property(value => value.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(value => value.TokenHash).IsUnique();
        builder.HasIndex(value => new { value.Email, value.CreatedAt });

        builder.HasOne(value => value.InvitedByUser)
            .WithMany()
            .HasForeignKey(value => value.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(value => value.AcceptedUser)
            .WithMany()
            .HasForeignKey(value => value.AcceptedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
