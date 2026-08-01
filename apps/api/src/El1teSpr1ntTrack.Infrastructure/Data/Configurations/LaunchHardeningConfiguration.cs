using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class AdminPasswordResetConfiguration : IEntityTypeConfiguration<AdminPasswordReset>
{
    public void Configure(EntityTypeBuilder<AdminPasswordReset> builder)
    {
        builder.Property(value => value.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(value => value.TokenHash).IsUnique();
        builder.HasIndex(value => new { value.UserId, value.ExpiresAtUtc });
        builder.HasOne(value => value.User).WithMany(value => value.PasswordResets)
            .HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AdminMfaChallengeConfiguration : IEntityTypeConfiguration<AdminMfaChallenge>
{
    public void Configure(EntityTypeBuilder<AdminMfaChallenge> builder)
    {
        builder.Property(value => value.ChallengeTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(value => value.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(value => value.ChallengeTokenHash).IsUnique();
        builder.HasIndex(value => new { value.UserId, value.ExpiresAtUtc });
        builder.HasOne(value => value.User).WithMany(value => value.MfaChallenges)
            .HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AuthenticationAttemptConfiguration : IEntityTypeConfiguration<AuthenticationAttempt>
{
    public void Configure(EntityTypeBuilder<AuthenticationAttempt> builder)
    {
        builder.Property(value => value.Purpose).HasMaxLength(40).IsRequired();
        builder.Property(value => value.PartitionHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(value => new { value.Purpose, value.PartitionHash, value.CreatedAt });
    }
}

public sealed class MediaDerivativeConfiguration : IEntityTypeConfiguration<MediaDerivative>
{
    public void Configure(EntityTypeBuilder<MediaDerivative> builder)
    {
        builder.Property(value => value.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(value => value.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(value => value.Sha256).HasMaxLength(64).IsRequired();
        builder.HasIndex(value => new { value.MediaAssetId, value.RequestedWidth }).IsUnique();
        builder.HasIndex(value => value.StorageKey).IsUnique();
        builder.HasOne(value => value.MediaAsset).WithMany(value => value.Derivatives)
            .HasForeignKey(value => value.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
    }
}
