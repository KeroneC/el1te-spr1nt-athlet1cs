using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

public sealed class ContactSubmissionConfiguration : IEntityTypeConfiguration<ContactSubmission>
{
    public void Configure(EntityTypeBuilder<ContactSubmission> builder)
    {
        builder.ConfigureCmsEntity("ContactSubmissions");
        builder.Property(submission => submission.Name).HasMaxLength(200).IsRequired();
        builder.Property(submission => submission.Email).HasMaxLength(256).IsRequired();
        builder.Property(submission => submission.Phone).HasMaxLength(30);
        builder.Property(submission => submission.InquiryType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(InquiryType.General)
            .IsRequired();
        builder.Property(submission => submission.Message).HasMaxLength(5000).IsRequired();
        builder.Property(submission => submission.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContactSubmissionStatus.New);
        builder.HasIndex(submission => submission.Status);
        builder.HasIndex(submission => submission.CreatedAtUtc);
    }
}
