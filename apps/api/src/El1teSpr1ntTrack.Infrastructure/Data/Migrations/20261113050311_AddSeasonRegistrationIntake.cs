using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonRegistrationIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrationSeasons",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OpeningDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AgeGroups = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PracticeLocation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PracticeSchedule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Fees = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ReplyWindow = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PaperworkEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PaperworkHandoff = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReviewOwner = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RetentionOwner = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurgedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSeasons", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationCounts",
                columns: table => new
                {
                    SeasonYear = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Visits = table.Column<long>(type: "bigint", nullable: false),
                    Applications = table.Column<int>(type: "int", nullable: false),
                    Returning = table.Column<int>(type: "int", nullable: false),
                    Enrolled = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationCounts", x => new { x.SeasonYear, x.Channel });
                    table.ForeignKey(
                        name: "FK_RegistrationCounts_RegistrationSeasons_SeasonYear",
                        column: x => x.SeasonYear,
                        principalTable: "RegistrationSeasons",
                        principalColumn: "Year",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonYear = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ReceiptReference = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestDigest = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    GuardianEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    GuardianName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuardianPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AthleteFirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AgeGroup = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Availability = table.Column<int>(type: "int", nullable: true),
                    Experience = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PriorClub = table.Column<int>(type: "int", nullable: true),
                    ClubName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Affiliations = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LastMeetMonth = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Transfer = table.Column<int>(type: "int", nullable: false),
                    PhotoConsent = table.Column<int>(type: "int", nullable: false),
                    CompetitionReady = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationSubmissions_RegistrationSeasons_SeasonYear",
                        column: x => x.SeasonYear,
                        principalTable: "RegistrationSeasons",
                        principalColumn: "Year",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Transfer = table.Column<int>(type: "int", nullable: false),
                    PhotoConsent = table.Column<int>(type: "int", nullable: false),
                    CompetitionReady = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationReviews_RegistrationSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "RegistrationSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationReviews_SubmissionId",
                table: "RegistrationReviews",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_ReceiptReference",
                table: "RegistrationSubmissions",
                column: "ReceiptReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_SeasonYear_GuardianEmail_SubmittedAtUtc",
                table: "RegistrationSubmissions",
                columns: new[] { "SeasonYear", "GuardianEmail", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_SeasonYear_Kind_Status_SubmittedAtUtc",
                table: "RegistrationSubmissions",
                columns: new[] { "SeasonYear", "Kind", "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_SeasonYear_RequestKey",
                table: "RegistrationSubmissions",
                columns: new[] { "SeasonYear", "RequestKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationCounts");

            migrationBuilder.DropTable(
                name: "RegistrationReviews");

            migrationBuilder.DropTable(
                name: "RegistrationSubmissions");

            migrationBuilder.DropTable(
                name: "RegistrationSeasons");
        }
    }
}
