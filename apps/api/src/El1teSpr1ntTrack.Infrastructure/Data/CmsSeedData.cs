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
        Block("20000000-0000-0000-0000-000000000007", "sponsors.intro", "Partner With Our Team", "Community partners help us provide equipment, coaching, meet access, and memorable experiences for young athletes.", 1)
    ];

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
