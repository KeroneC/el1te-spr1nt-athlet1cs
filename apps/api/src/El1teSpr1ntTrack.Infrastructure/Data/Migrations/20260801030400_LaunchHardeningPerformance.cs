using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LaunchHardeningPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedLoginWindowStartedUtc",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSuccessfulLoginUtc",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEndUtc",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityVersion",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AdminMfaChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "int", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminMfaChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminMfaChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminPasswordResets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPasswordResets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminPasswordResets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PartitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaDerivatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedWidth = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaDerivatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaDerivatives_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ContentBlocks",
                columns: new[] { "Id", "Body", "CreatedAtUtc", "CtaText", "CtaUrl", "DisplayOrder", "ImageUrl", "IsPublished", "Key", "Summary", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000008"), "We collect only the information needed to respond to inquiries, manage authorized club content, and operate approved services. Public performance analytics are configured without cookies or user identifiers. We do not sell personal information. Authorized staff may access submitted information only for club operations, support, safety, and legal obligations. Contact the club to ask about access, correction, or removal. This draft must be approved by the organization before public launch.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 1, null, true, "policy.privacy", null, "Privacy", null },
                    { new Guid("20000000-0000-0000-0000-000000000009"), "El1te Spr1nt Athlet1cs aims to provide a website that works with keyboards, screen readers, zoom, reduced motion, and common mobile devices. If you encounter a barrier, contact the club with the page, approximate time, and a description of the problem so staff can investigate and provide an alternative. This draft must be approved by the organization before public launch.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 2, null, true, "policy.accessibility", null, "Accessibility", null },
                    { new Guid("20000000-0000-0000-0000-000000000010"), "This website provides club information and administrative tools. Content may change as schedules, programs, eligibility, and availability are reviewed. Do not misuse the site, attempt unauthorized access, or submit information you are not authorized to provide. External services and links have their own terms. This draft is factual operational guidance, not legal advice, and must be approved before public launch.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 3, null, true, "policy.terms", null, "Website Terms", null },
                    { new Guid("20000000-0000-0000-0000-000000000011"), "The merchandise shop is currently a preview and does not accept payment. Before launch, final prices, tax, availability, handoff arrangements, customization review, cancellations, returns, and refunds will be shown before checkout. Card details will be handled by Square and not stored by El1te. This draft must be updated and approved before payments are enabled.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, 4, null, true, "policy.store", null, "Store Policy", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LockoutEndUtc",
                table: "Users",
                column: "LockoutEndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminMfaChallenges_ChallengeTokenHash",
                table: "AdminMfaChallenges",
                column: "ChallengeTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminMfaChallenges_UserId_ExpiresAtUtc",
                table: "AdminMfaChallenges",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminPasswordResets_TokenHash",
                table: "AdminPasswordResets",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminPasswordResets_UserId_ExpiresAtUtc",
                table: "AdminPasswordResets",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAttempts_Purpose_PartitionHash_CreatedAt",
                table: "AuthenticationAttempts",
                columns: new[] { "Purpose", "PartitionHash", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaDerivatives_MediaAssetId_RequestedWidth",
                table: "MediaDerivatives",
                columns: new[] { "MediaAssetId", "RequestedWidth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaDerivatives_StorageKey",
                table: "MediaDerivatives",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminMfaChallenges");

            migrationBuilder.DropTable(
                name: "AdminPasswordResets");

            migrationBuilder.DropTable(
                name: "AuthenticationAttempts");

            migrationBuilder.DropTable(
                name: "MediaDerivatives");

            migrationBuilder.DropIndex(
                name: "IX_Users_LockoutEndUtc",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"));

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginWindowStartedUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulLoginUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEndUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                table: "Users");
        }
    }
}
