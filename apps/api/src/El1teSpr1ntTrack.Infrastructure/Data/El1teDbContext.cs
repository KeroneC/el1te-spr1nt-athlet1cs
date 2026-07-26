using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Data;

public sealed class El1teDbContext(DbContextOptions<El1teDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<AdminInvitation> AdminInvitations => Set<AdminInvitation>();

    public DbSet<AdminActivityLog> AdminActivityLogs => Set<AdminActivityLog>();

    public DbSet<Athlete> Athletes => Set<Athlete>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();

    public DbSet<Announcement> Announcements => Set<Announcement>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Coach> Coaches => Set<Coach>();

    public DbSet<Sponsor> Sponsors => Set<Sponsor>();

    public DbSet<Faq> Faqs => Set<Faq>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();

    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductVariantOptionValue> ProductVariantOptionValues => Set<ProductVariantOptionValue>();

    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();

    public DbSet<ProductModifierValue> ProductModifierValues => Set<ProductModifierValue>();

    public DbSet<ProductVisualizerLayer> ProductVisualizerLayers => Set<ProductVisualizerLayer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();

    public DbSet<InventoryReservationItem> InventoryReservationItems => Set<InventoryReservationItem>();

    public DbSet<CommerceRefund> CommerceRefunds => Set<CommerceRefund>();

    public DbSet<OrderInternalNote> OrderInternalNotes => Set<OrderInternalNote>();

    public DbSet<CommerceEmailMessage> CommerceEmailMessages => Set<CommerceEmailMessage>();

    public DbSet<SquareWebhookEvent> SquareWebhookEvents => Set<SquareWebhookEvent>();

    public DbSet<CommerceOutboxMessage> CommerceOutboxMessages => Set<CommerceOutboxMessage>();

    public DbSet<Donation> Donations => Set<Donation>();

    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<GalleryAlbum> GalleryAlbums => Set<GalleryAlbum>();

    public DbSet<GalleryAlbumMedia> GalleryAlbumMedia => Set<GalleryAlbumMedia>();

    public DbSet<FeedbackSubmission> FeedbackSubmissions => Set<FeedbackSubmission>();

    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    public DbSet<AthleteDocument> AthleteDocuments => Set<AthleteDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(El1teDbContext).Assembly);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(user => user.Email)
            .HasMaxLength(256);

        modelBuilder.Entity<User>()
            .Property(user => user.FirstName)
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .Property(user => user.LastName)
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .Property(user => user.PasswordHash)
            .HasMaxLength(512);

        modelBuilder.Entity<User>()
            .HasMany(user => user.Athletes)
            .WithOne(athlete => athlete.ParentUser)
            .HasForeignKey(athlete => athlete.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(user => user.Orders)
            .WithOne(order => order.User)
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Athlete>()
            .HasMany(athlete => athlete.ConsentRecords)
            .WithOne(consent => consent.Athlete)
            .HasForeignKey(consent => consent.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Athlete>()
            .HasMany(athlete => athlete.AthleteDocuments)
            .WithOne(document => document.Athlete)
            .HasForeignKey(document => document.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Donation>()
            .Property(donation => donation.Amount)
            .HasPrecision(18, 2);
    }
}
