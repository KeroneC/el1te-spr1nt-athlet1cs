using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;

namespace El1teSpr1ntTrack.Infrastructure.Data;

public static class CmsSeedData
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly SiteSetting[] SiteSettings =
    [
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            ClubName = "El1te Spr1nt Athlet1cs",
            Slogan = "Greatness begins here; hang on for the ride!",
            ContactEmail = "contact@example.test",
            FacebookUrl = "https://example.test/el1tespr1nt/facebook",
            InstagramUrl = "https://example.test/el1tespr1nt/instagram",
            YouTubeUrl = "https://example.test/el1tespr1nt/youtube",
            PrimaryCtaText = "Join the Club",
            PrimaryCtaUrl = "/registration",
            SecondaryCtaText = "Support Us",
            SecondaryCtaUrl = "/sponsors",
            CreatedAtUtc = CreatedAtUtc
        }
    ];

    public static readonly ContentBlock[] ContentBlocks =
    [
        Block("20000000-0000-0000-0000-000000000001", "home.hero", "Greatness Begins Here", "A welcoming track club where young athletes build speed, confidence, and character.", 1),
        Block("20000000-0000-0000-0000-000000000002", "home.mission", "Our Mission", "We create an encouraging environment where every athlete can learn, compete, and grow.", 2),
        Block("20000000-0000-0000-0000-000000000003", "home.programs", "Programs for Every Step", "From first practices to competitive meets, our programs support athletes at each stage of development.", 3),
        Block("20000000-0000-0000-0000-000000000004", "about.story", "Our Mission", "El1te Spr1nt Athlet1cs offers a track and field developmental program that includes preseason strength and conditioning workouts, with the goal of participating in track meets held in the spring and summer each year. We compete locally, regionally, and nationally in track and field events sanctioned by USATF and AAU. Our club is a nonprofit organization formed with the mission of promoting track and field for youth ages 7 to 18 in our local area. By doing so, we provide an avenue for each athlete to enhance their talent while achieving whatever life goals they may have set for themselves. This program is not a recreational program; rather, it is designed to empower young competitive athletes by teaching basic running skills, body mechanics, event fundamentals, sportsmanship, and discipline.", 1),
        Block("20000000-0000-0000-0000-000000000005", "about.values", "What We Value", "Effort, respect, teamwork, discipline, and joy guide how we train and compete together.", 2),
        Block("20000000-0000-0000-0000-000000000006", "registration.intro", "Ready to Run?", "Registration information, season details, and athlete requirements will be available here.", 1),
        Block("20000000-0000-0000-0000-000000000007", "sponsors.intro", "Partner With Our Team", "Community partners help us provide equipment, coaching, meet access, and memorable experiences for young athletes.", 1),
        Block("20000000-0000-0000-0000-000000000008", "policy.privacy", "Privacy Policy", PrivacyPolicy, 1),
        Block("20000000-0000-0000-0000-000000000009", "policy.accessibility", "Accessibility Statement", AccessibilityPolicy, 2),
        Block("20000000-0000-0000-0000-000000000010", "policy.terms", "Website Terms", TermsPolicy, 3),
        Block("20000000-0000-0000-0000-000000000011", "policy.store", "Store Policy", StorePolicy, 4)
    ];

    private static string PrivacyPolicy => """
        Last updated: August 4, 2026

        ## Information we collect

        When you contact El1te Spr1nt Athlet1cs, we collect the name, email address, optional phone number, inquiry details, and message you provide. Store checkout collects the adult buyer's name, email, phone number, delivery address when shipping is required, product selections, order history, fulfillment status, tracking, cancellations, and refunds.

        ## Payments and fulfillment

        Card details are entered on Square and are never received or stored by El1te. Printify receives the customer and order information required to manufacture and deliver Printify items. Delivery carriers receive the information required to deliver packages.

        Azure hosts the application and supports transactional email. Authorized staff may access submitted information only for club operations, support, security, accounting, safety, and legal obligations.

        ## Browser storage and analytics

        The public cart stores only non-personal product configuration in the browser. Essential security and checkout-return cookies may be used. Public performance analytics are cookie-free and exclude names, emails, addresses, form contents, cart details, and Admin activity.

        We do not sell or rent personal information.

        ## Retention and security

        Records are kept only as reasonably needed for fulfillment, accounting, disputes, safety, security, and legal obligations. We use reasonable safeguards, but no internet service can promise absolute security.

        ## Children and families

        Store purchases must be made by an adult. A parent or guardian should submit information involving a youth athlete.

        ## Your choices

        To request access, correction, or deletion, email [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com). Some records may need to be retained for accounting, safety, dispute, or legal reasons.
        """.ReplaceLineEndings("\n");

    private static string AccessibilityPolicy => """
        Last updated: August 4, 2026

        ## Our commitment

        El1te Spr1nt Athlet1cs is working toward WCAG 2.2 Level AA so the website can be used by as many people as possible.

        Our ongoing work includes:

        - Keyboard access and visible focus
        - Screen-reader support and meaningful alternative text
        - Browser zoom and responsive layouts
        - Reduced-motion support
        - Clear labels, instructions, validation, and error messages

        ## Third-party services

        Square and Printify provide parts of the payment and fulfillment experience. We do not control every part of those third-party services, but we will help identify a practical alternative when possible.

        ## Report a barrier

        Email [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with the page, device or browser, approximate time, and a description of the problem. Please do not include payment information or sensitive athlete information.

        We will investigate and respond as soon as practical. Accessibility is an ongoing responsibility, and this statement will be updated as the website and its services change.
        """.ReplaceLineEndings("\n");

    private static string TermsPolicy => """
        Last updated: August 4, 2026

        ## Using this website

        By using this website, you agree to these terms and, when making a purchase, the Store Policy. Club schedules, programs, eligibility, prices, inventory, and other content may change as information is reviewed.

        Store buyers must be at least 18 years old and must provide accurate contact, payment, and fulfillment information.

        ## Acceptable use

        Do not attempt unauthorized access, disrupt the service, impersonate another person, submit information you are not authorized to provide, use automated requests that place an unreasonable burden on the service, or use club or sponsor intellectual property without permission.

        ## Content and third parties

        El1te owns or is authorized to use the website's content and branding. Viewing the website does not grant permission to copy or reuse that material.

        Square, Printify, delivery carriers, sponsors, and linked websites operate under their own terms and policies. We will correct known website errors where practical, but we cannot promise uninterrupted service or guarantee a third party's availability or performance.

        ## Questions and updates

        These limitations apply only to the extent permitted by law. We may update these terms as the website changes. Questions may be sent to [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com).
        """.ReplaceLineEndings("\n");

    private static string StorePolicy => """
        Last updated: August 4, 2026

        ## Prices, payment, and delivery

        Prices are in U.S. dollars. Square processes payment and calculates configured taxes. Card details are not stored by El1te.

        Printify products include free standard shipping to valid United States addresses. International and expedited shipping are not available at launch. Club-stock products are delivered through an arranged practice or event handoff and are not shipped by staff. A mixed order is paid together but its items arrive separately.

        ## Review and cancellation

        Review the address, size, color, and approved-design choices before payment. You may cancel the complete order and receive a full Square refund from the secure order-status page during the 30-minute production hold. After production release begins, cancellation is not guaranteed.

        Correcting a delivery address after payment requires canceling within the hold window and placing a new order.

        ## Returns and product problems

        Printify items are made to order. We do not accept returns because the wrong size or color was selected or because the buyer changed their mind. Report a damaged, misprinted, or incorrect item within 30 days of delivery with the order reference and photographs so we can investigate and arrange an appropriate replacement or refund.

        Unworn, unwashed club-stock items in original condition may be returned or exchanged within 14 days of handoff, subject to available stock.

        Refunds return to the original Square payment method. Processing time after a refund is submitted depends on the buyer's financial institution.

        ## Timing and available choices

        Production and delivery dates are estimates. We will help investigate delayed or lost shipments but cannot guarantee carrier timelines.

        Launch products offer only the listed size, garment-color, and approved-design choices. Free-form names, numbers, and custom artwork are not available.

        ## Store support

        Email [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with your order reference. Do not send card details by email.
        """.ReplaceLineEndings("\n");

    public static readonly Announcement[] Announcements =
    [
        new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Title = "Summer Registration Is Open",
            Slug = "summer-registration-is-open",
            Summary = "Families can now register athletes for the upcoming summer track season.",
            Body = "Review the season information and complete registration before available roster spaces are filled.",
            IsFeatured = true,
            IsPublished = true,
            PublishDateUtc = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            ExpirationDateUtc = new DateTimeOffset(2026, 7, 15, 23, 59, 0, TimeSpan.Zero),
            CreatedAtUtc = CreatedAtUtc
        },
        new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Title = "Practice Schedule Update",
            Slug = "practice-schedule-update",
            Summary = "Weeknight practice times have been adjusted for the summer schedule.",
            Body = "Check the team calendar before arriving and allow extra time for athlete check-in.",
            IsPublished = true,
            PublishDateUtc = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            CreatedAtUtc = CreatedAtUtc
        },
        new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            Title = "Community Fundraiser Announced",
            Slug = "community-fundraiser-announced",
            Summary = "Join the club for a community fundraiser supporting athlete equipment and meet fees.",
            Body = "Families, supporters, and local partners are invited to participate and help expand access to youth track and field.",
            IsPublished = true,
            PublishDateUtc = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero),
            CreatedAtUtc = CreatedAtUtc
        }
    ];

    public static readonly Event[] Events =
    [
        new()
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            Title = "Summer Team Practice",
            Slug = "summer-team-practice",
            EventType = EventType.Practice,
            StartDateTimeUtc = new DateTimeOffset(2026, 7, 7, 22, 0, 0, TimeSpan.Zero),
            EndDateTimeUtc = new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
            LocationName = "Community Track",
            Address = "100 Track Lane",
            Description = "A full-team practice focused on sprint mechanics, starts, and age-group conditioning.",
            IsPublished = true,
            CreatedAtUtc = CreatedAtUtc
        },
        new()
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
            Title = "Regional Youth Track Meet",
            Slug = "regional-youth-track-meet",
            EventType = EventType.Meet,
            StartDateTimeUtc = new DateTimeOffset(2026, 7, 18, 13, 0, 0, TimeSpan.Zero),
            EndDateTimeUtc = new DateTimeOffset(2026, 7, 18, 21, 0, 0, TimeSpan.Zero),
            LocationName = "Regional Athletics Complex",
            Address = "200 Victory Way",
            Description = "A regional competition featuring sprint, relay, distance, and field events.",
            RegistrationUrl = "https://example.test/meets/regional-youth",
            IsFeatured = true,
            IsPublished = true,
            CreatedAtUtc = CreatedAtUtc
        },
        new()
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
            Title = "Run for the Future Fundraiser",
            Slug = "run-for-the-future-fundraiser",
            EventType = EventType.Fundraiser,
            StartDateTimeUtc = new DateTimeOffset(2026, 8, 1, 15, 0, 0, TimeSpan.Zero),
            EndDateTimeUtc = new DateTimeOffset(2026, 8, 1, 19, 0, 0, TimeSpan.Zero),
            LocationName = "Community Recreation Center",
            Address = "300 Community Drive",
            Description = "A family-friendly fundraiser supporting uniforms, equipment, and athlete meet fees.",
            IsPublished = true,
            CreatedAtUtc = CreatedAtUtc
        },
        new()
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
            Title = "Summer Registration Deadline",
            Slug = "summer-registration-deadline",
            EventType = EventType.RegistrationDeadline,
            StartDateTimeUtc = new DateTimeOffset(2026, 7, 15, 23, 59, 0, TimeSpan.Zero),
            LocationName = "Online",
            Description = "Complete athlete registration by this deadline to be considered for the summer roster.",
            RegistrationUrl = "/registration",
            IsPublished = true,
            CreatedAtUtc = CreatedAtUtc
        }
    ];

    public static readonly Coach[] Coaches =
    [
        Coach("50000000-0000-0000-0000-000000000001", "Jordan", "Taylor", "Head Coach", "A youth development coach focused on fundamentals, confidence, and positive competition.", 1),
        Coach("50000000-0000-0000-0000-000000000002", "Morgan", "Lee", "Sprints Coach", "A sprint coach who helps athletes improve mechanics, acceleration, and race preparation.", 2),
        Coach("50000000-0000-0000-0000-000000000003", "Casey", "Rivera", "Team Support Coach", "A team support coach committed to safe practices, encouragement, and athlete growth.", 3)
    ];

    public static readonly HallOfFameInductee[] HallOfFameInductees =
    [
        new()
        {
            Id = Guid.Parse("58000000-0000-0000-0000-000000000001"),
            Name = "Dani Prunzik",
            Slug = "dani-prunzik",
            Affiliation = "Penn State University",
            Summary = "Upper St. Clair High School class of 2023 graduate, Penn State student, and talented sprinter with a 60m indoor PR of 7.57.",
            PhotoUrl = "/images/hall-of-fame/dani-prunzik.jpeg",
            PhotoAlt = "Dani Prunzik holding an American flag in her Penn State track uniform",
            DisplayOrder = 1,
            IsActive = true,
            CreatedAtUtc = CreatedAtUtc
        },
        new()
        {
            Id = Guid.Parse("58000000-0000-0000-0000-000000000002"),
            Name = "Kaitlyn Eger",
            Slug = "kaitlyn-eger",
            Affiliation = "Youngstown State University",
            Summary = "Youngstown State University student-athlete studying Exercise Science (Pre-PT). A multi-time top-5 Horizon League finisher and Meet MVP who helped lead back-to-back conference championships in 2024 and 2025.",
            PhotoUrl = "/images/hall-of-fame/kaitlyn-eger.jpg",
            PhotoAlt = "Kaitlyn Eger posing with a pole vault pole in her Youngstown State uniform",
            DisplayOrder = 2,
            IsActive = true,
            CreatedAtUtc = CreatedAtUtc
        }
    ];

    public static readonly Sponsor[] Sponsors =
    [
        Sponsor("60000000-0000-0000-0000-000000000001", "Community Health Partners", "community-health-partners", SponsorTier.Platinum, 1),
        Sponsor("60000000-0000-0000-0000-000000000002", "Victory Lane Athletics", "victory-lane-athletics", SponsorTier.Gold, 2),
        Sponsor("60000000-0000-0000-0000-000000000003", "Neighborhood Family Market", "neighborhood-family-market", SponsorTier.Silver, 3),
        Sponsor("60000000-0000-0000-0000-000000000004", "Friends of Youth Sports", "friends-of-youth-sports", SponsorTier.Community, 4)
    ];

    public static readonly Faq[] Faqs =
    [
        Faq("70000000-0000-0000-0000-000000000001", "What ages can join?", "Available age groups may vary by season. Registration details will list the current eligible ages.", "Registration", 1),
        Faq("70000000-0000-0000-0000-000000000002", "How do I register my child?", "Complete the online registration form and provide any required documents before the season deadline.", "Registration", 2),
        Faq("70000000-0000-0000-0000-000000000003", "What should athletes bring to practice?", "Athletes should bring water, weather-appropriate training clothes, running shoes, and any coach-requested equipment.", "Practices", 3),
        Faq("70000000-0000-0000-0000-000000000004", "Do athletes need prior experience?", "No. Coaches support beginners and experienced athletes with age-appropriate instruction and training.", "Programs", 4),
        Faq("70000000-0000-0000-0000-000000000005", "How can I sponsor the club?", "Use the contact form and select the sponsor inquiry type to begin a partnership conversation.", "Support", 5),
        Faq("70000000-0000-0000-0000-000000000006", "How can I volunteer?", "Use the contact form and select the volunteer inquiry type to share your interests and availability.", "Support", 6)
    ];

    private static ContentBlock Block(string id, string key, string title, string body, int order)
    {
        return new ContentBlock
        {
            Id = Guid.Parse(id),
            Key = key,
            Title = title,
            Body = body,
            DisplayOrder = order,
            IsPublished = true,
            CreatedAtUtc = CreatedAtUtc
        };
    }

    private static Coach Coach(string id, string firstName, string lastName, string role, string bio, int order)
    {
        return new Coach
        {
            Id = Guid.Parse(id),
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            Bio = bio,
            DisplayOrder = order,
            IsActive = true,
            CreatedAtUtc = CreatedAtUtc
        };
    }

    private static Sponsor Sponsor(string id, string name, string slug, SponsorTier tier, int order)
    {
        return new Sponsor
        {
            Id = Guid.Parse(id),
            Name = name,
            Slug = slug,
            Tier = tier,
            Description = "Placeholder sponsor profile for local CMS development.",
            DisplayOrder = order,
            IsActive = true,
            CreatedAtUtc = CreatedAtUtc
        };
    }

    private static Faq Faq(string id, string question, string answer, string category, int order)
    {
        return new Faq
        {
            Id = Guid.Parse(id),
            Question = question,
            Answer = answer,
            Category = category,
            DisplayOrder = order,
            IsActive = true,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}
