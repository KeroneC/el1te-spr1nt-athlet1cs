using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class RegistrationConfiguration : IEntityTypeConfiguration<RegistrationSeason>,
    IEntityTypeConfiguration<RegistrationSubmission>, IEntityTypeConfiguration<RegistrationCount>
{
    public void Configure(EntityTypeBuilder<RegistrationSeason> builder)
    {
        builder.HasKey(x => x.Year);
        builder.Property(x => x.Year).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(120);
        builder.Property(x => x.AgeGroups).HasMaxLength(400);
        builder.Property(x => x.PracticeLocation).HasMaxLength(250);
        builder.Property(x => x.PracticeSchedule).HasMaxLength(500);
        builder.Property(x => x.Fees).HasMaxLength(250);
        builder.Property(x => x.ReplyWindow).HasMaxLength(250);
        builder.Property(x => x.PaperworkEmail).HasMaxLength(254);
        builder.Property(x => x.PaperworkHandoff).HasMaxLength(500);
        builder.Property(x => x.ReviewOwner).HasMaxLength(120);
        builder.Property(x => x.RetentionOwner).HasMaxLength(120);
        builder.Property(x => x.Revision).IsConcurrencyToken();
    }

    public void Configure(EntityTypeBuilder<RegistrationSubmission> builder)
    {
        builder.HasIndex(x => new { x.SeasonYear, x.RequestKey }).IsUnique();
        builder.HasIndex(x => x.ReceiptReference).IsUnique();
        builder.HasIndex(x => new { x.SeasonYear, x.Kind, x.Status, x.SubmittedAtUtc });
        builder.HasIndex(x => new { x.SeasonYear, x.GuardianEmail, x.SubmittedAtUtc });
        builder.HasOne<RegistrationSeason>().WithMany().HasForeignKey(x => x.SeasonYear).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.History).WithOne().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.Property(x => x.ReceiptReference).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.RequestDigest).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.GuardianEmail).HasMaxLength(254);
        builder.Property(x => x.GuardianName).HasMaxLength(100);
        builder.Property(x => x.GuardianPhone).HasMaxLength(32);
        builder.Property(x => x.AthleteFirstName).HasMaxLength(80);
        builder.Property(x => x.AgeGroup).HasMaxLength(40);
        builder.Property(x => x.Experience).HasMaxLength(250);
        builder.Property(x => x.ClubName).HasMaxLength(120);
        builder.Property(x => x.Affiliations).HasMaxLength(40);
        builder.Property(x => x.LastMeetMonth).HasMaxLength(7).IsUnicode(false);
    }

    public void Configure(EntityTypeBuilder<RegistrationCount> builder)
    {
        builder.HasKey(x => new { x.SeasonYear, x.Channel });
        builder.HasOne<RegistrationSeason>().WithMany().HasForeignKey(x => x.SeasonYear).OnDelete(DeleteBehavior.Restrict);
    }
}
