using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCmsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Events",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Events",
                newName: "LocationName");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Events",
                newName: "Title");

            migrationBuilder.DropColumn(
                name: "RegistrationFee",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ContactSubmissions",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Events",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartsAt",
                table: "Events",
                newName: "StartDateTimeUtc");

            migrationBuilder.RenameColumn(
                name: "EndsAt",
                table: "Events",
                newName: "EndDateTimeUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ContactSubmissions",
                newName: "UpdatedAtUtc");

            migrationBuilder.Sql(
                "UPDATE [Events] SET [LocationName] = 'Location to be announced' WHERE [LocationName] IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "Events",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "LocationName",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "ContactSubmissions",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Events",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Events",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationUrl",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE [Events] SET [Slug] = CONCAT('legacy-event-', REPLACE(CONVERT(varchar(36), [Id]), '-', '')) WHERE [Slug] = '';");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContactSubmissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ContactSubmissions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "InquiryType",
                table: "ContactSubmissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ContactSubmissions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ContactSubmissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "New");

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpirationDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsEmailPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CtaText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CtaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slogan = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstagramUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YouTubeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimaryCtaText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimaryCtaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecondaryCtaText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecondaryCtaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sponsors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsors", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Announcements",
                columns: new[] { "Id", "Body", "CreatedAtUtc", "ExpirationDateUtc", "ImageUrl", "IsFeatured", "IsPublished", "PublishDateUtc", "Slug", "Summary", "Title", "UpdatedAtUtc" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), "Review the season information and complete registration before available roster spaces are filled.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 15, 23, 59, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, new DateTimeOffset(new DateTime(2026, 6, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "summer-registration-is-open", "Families can now register athletes for the upcoming summer track season.", "Summer Registration Is Open", null });

            migrationBuilder.InsertData(
                table: "Announcements",
                columns: new[] { "Id", "Body", "CreatedAtUtc", "ExpirationDateUtc", "ImageUrl", "IsPublished", "PublishDateUtc", "Slug", "Summary", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000002"), "Check the team calendar before arriving and allow extra time for athlete check-in.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, new DateTimeOffset(new DateTime(2026, 6, 8, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "practice-schedule-update", "Weeknight practice times have been adjusted for the summer schedule.", "Practice Schedule Update", null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "Families, supporters, and local partners are invited to participate and help expand access to youth track and field.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, new DateTimeOffset(new DateTime(2026, 6, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "community-fundraiser-announced", "Join the club for a community fundraiser supporting athlete equipment and meet fees.", "Community Fundraiser Announced", null }
                });

            migrationBuilder.InsertData(
                table: "Coaches",
                columns: new[] { "Id", "Bio", "CreatedAtUtc", "DisplayOrder", "Email", "FirstName", "ImageUrl", "IsActive", "LastName", "Role", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), "A youth development coach focused on fundamentals, confidence, and positive competition.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, null, "Jordan", null, true, "Taylor", "Head Coach", null },
                    { new Guid("50000000-0000-0000-0000-000000000002"), "A sprint coach who helps athletes improve mechanics, acceleration, and race preparation.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, null, "Morgan", null, true, "Lee", "Sprints Coach", null },
                    { new Guid("50000000-0000-0000-0000-000000000003"), "A team support coach committed to safe practices, encouragement, and athlete growth.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, null, "Casey", null, true, "Rivera", "Team Support Coach", null }
                });

            migrationBuilder.InsertData(
                table: "ContentBlocks",
                columns: new[] { "Id", "Body", "CreatedAtUtc", "CtaText", "CtaUrl", "DisplayOrder", "ImageUrl", "IsPublished", "Key", "Summary", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "A welcoming track club where young athletes build speed, confidence, and character.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 1, null, true, "home.hero", null, "Greatness Begins Here", null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "We create an encouraging environment where every athlete can learn, compete, and grow.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 2, null, true, "home.mission", null, "Our Mission", null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "From first practices to competitive meets, our programs support athletes at each stage of development.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 3, null, true, "home.programs", null, "Programs for Every Step", null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "El1te Spr1nt Athlet1cs was built to make quality track and field opportunities accessible to young athletes and their families.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 1, null, true, "about.story", null, "Our Story", null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "Effort, respect, teamwork, discipline, and joy guide how we train and compete together.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 2, null, true, "about.values", null, "What We Value", null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "Registration information, season details, and athlete requirements will be available here.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 1, null, true, "registration.intro", null, "Ready to Run?", null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "Community partners help us provide equipment, coaching, meet access, and memorable experiences for young athletes.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 1, null, true, "sponsors.intro", null, "Partner With Our Team", null }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Address", "CreatedAtUtc", "Description", "EndDateTimeUtc", "ImageUrl", "IsPublished", "LocationName", "RegistrationUrl", "Slug", "StartDateTimeUtc", "Title", "UpdatedAtUtc" },
                values: new object[] { new Guid("40000000-0000-0000-0000-000000000001"), "100 Track Lane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A full-team practice focused on sprint mechanics, starts, and age-group conditioning.", new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Community Track", null, "summer-team-practice", new DateTimeOffset(new DateTime(2026, 7, 7, 22, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Summer Team Practice", null });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Address", "CreatedAtUtc", "Description", "EndDateTimeUtc", "EventType", "ImageUrl", "IsFeatured", "IsPublished", "LocationName", "RegistrationUrl", "Slug", "StartDateTimeUtc", "Title", "UpdatedAtUtc" },
                values: new object[] { new Guid("40000000-0000-0000-0000-000000000002"), "200 Victory Way", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A regional competition featuring sprint, relay, distance, and field events.", new DateTimeOffset(new DateTime(2026, 7, 18, 21, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Meet", null, true, true, "Regional Athletics Complex", "https://example.test/meets/regional-youth", "regional-youth-track-meet", new DateTimeOffset(new DateTime(2026, 7, 18, 13, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Regional Youth Track Meet", null });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Address", "CreatedAtUtc", "Description", "EndDateTimeUtc", "EventType", "ImageUrl", "IsPublished", "LocationName", "RegistrationUrl", "Slug", "StartDateTimeUtc", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000003"), "300 Community Drive", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A family-friendly fundraiser supporting uniforms, equipment, and athlete meet fees.", new DateTimeOffset(new DateTime(2026, 8, 1, 19, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fundraiser", null, true, "Community Recreation Center", null, "run-for-the-future-fundraiser", new DateTimeOffset(new DateTime(2026, 8, 1, 15, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Run for the Future Fundraiser", null },
                    { new Guid("40000000-0000-0000-0000-000000000004"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Complete athlete registration by this deadline to be considered for the summer roster.", null, "RegistrationDeadline", null, true, "Online", "/registration", "summer-registration-deadline", new DateTimeOffset(new DateTime(2026, 7, 15, 23, 59, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Summer Registration Deadline", null }
                });

            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "Category", "CreatedAtUtc", "DisplayOrder", "IsActive", "Question", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("70000000-0000-0000-0000-000000000001"), "Available age groups may vary by season. Registration details will list the current eligible ages.", "Registration", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, true, "What ages can join?", null },
                    { new Guid("70000000-0000-0000-0000-000000000002"), "Complete the online registration form and provide any required documents before the season deadline.", "Registration", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, true, "How do I register my child?", null },
                    { new Guid("70000000-0000-0000-0000-000000000003"), "Athletes should bring water, weather-appropriate training clothes, running shoes, and any coach-requested equipment.", "Practices", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, true, "What should athletes bring to practice?", null },
                    { new Guid("70000000-0000-0000-0000-000000000004"), "No. Coaches support beginners and experienced athletes with age-appropriate instruction and training.", "Programs", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, true, "Do athletes need prior experience?", null },
                    { new Guid("70000000-0000-0000-0000-000000000005"), "Use the contact form and select the sponsor inquiry type to begin a partnership conversation.", "Support", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 5, true, "How can I sponsor the club?", null },
                    { new Guid("70000000-0000-0000-0000-000000000006"), "Use the contact form and select the volunteer inquiry type to share your interests and availability.", "Support", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 6, true, "How can I volunteer?", null }
                });

            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Id", "AddressLine1", "AddressLine2", "City", "ClubName", "ContactEmail", "CreatedAtUtc", "FacebookUrl", "InstagramUrl", "LogoUrl", "PhoneNumber", "PrimaryCtaText", "PrimaryCtaUrl", "SecondaryCtaText", "SecondaryCtaUrl", "Slogan", "State", "UpdatedAtUtc", "YouTubeUrl", "ZipCode" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000001"), null, null, null, "El1te Spr1nt Athlet1cs", "contact@example.test", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://example.test/el1tespr1nt/facebook", "https://example.test/el1tespr1nt/instagram", null, null, "Join the Club", "/registration", "Support Us", "/sponsors", "Greatness begins here; hang on for the ride!", null, null, "https://example.test/el1tespr1nt/youtube", null });

            migrationBuilder.InsertData(
                table: "Sponsors",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "DisplayOrder", "IsActive", "LogoUrl", "Name", "Slug", "Tier", "UpdatedAtUtc", "WebsiteUrl" },
                values: new object[,]
                {
                    { new Guid("60000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Placeholder sponsor profile for local CMS development.", 1, true, null, "Community Health Partners", "community-health-partners", "Platinum", null, null },
                    { new Guid("60000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Placeholder sponsor profile for local CMS development.", 2, true, null, "Victory Lane Athletics", "victory-lane-athletics", "Gold", null, null },
                    { new Guid("60000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Placeholder sponsor profile for local CMS development.", 3, true, null, "Neighborhood Family Market", "neighborhood-family-market", "Silver", null, null },
                    { new Guid("60000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Placeholder sponsor profile for local CMS development.", 4, true, null, "Friends of Youth Sports", "friends-of-youth-sports", "Community", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventType",
                table: "Events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished",
                table: "Events",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Slug",
                table: "Events",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartDateTimeUtc",
                table: "Events",
                column: "StartDateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ContactSubmissions_CreatedAtUtc",
                table: "ContactSubmissions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ContactSubmissions_Status",
                table: "ContactSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsPublished",
                table: "Announcements",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_PublishDateUtc",
                table: "Announcements",
                column: "PublishDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Slug",
                table: "Announcements",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coaches_DisplayOrder",
                table: "Coaches",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ContentBlocks_Key",
                table: "ContentBlocks",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_DisplayOrder",
                table: "Faqs",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_Slug",
                table: "Sponsors",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Coaches");

            migrationBuilder.DropTable(
                name: "ContentBlocks");

            migrationBuilder.DropTable(
                name: "Faqs");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "Sponsors");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventType",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsPublished",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Slug",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_StartDateTimeUtc",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_ContactSubmissions_CreatedAtUtc",
                table: "ContactSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ContactSubmissions_Status",
                table: "ContactSubmissions");

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "InquiryType",
                table: "ContactSubmissions");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ContactSubmissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ContactSubmissions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "Events",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "LocationName",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "ContactSubmissions",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Events",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "LocationName",
                table: "Events",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Events",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Events",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "StartDateTimeUtc",
                table: "Events",
                newName: "StartsAt");

            migrationBuilder.RenameColumn(
                name: "EndDateTimeUtc",
                table: "Events",
                newName: "EndsAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "ContactSubmissions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ContactSubmissions",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationFee",
                table: "Events",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContactSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ContactSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

        }
    }
}
