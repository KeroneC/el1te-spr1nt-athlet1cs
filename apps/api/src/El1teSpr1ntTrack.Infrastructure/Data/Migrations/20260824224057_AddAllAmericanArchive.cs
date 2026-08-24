using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllAmericanArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllAmericanYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AthleteCount = table.Column<int>(type: "int", nullable: false),
                    MedalCount = table.Column<int>(type: "int", nullable: false),
                    HeroMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DetailsComplete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllAmericanYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllAmericanYears_MediaAssets_HeroMediaAssetId",
                        column: x => x.HeroMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllAmericanPerformances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllAmericanYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Division = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Placement = table.Column<int>(type: "int", nullable: true),
                    IsRelay = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllAmericanPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllAmericanPerformances_AllAmericanYears_AllAmericanYearId",
                        column: x => x.AllAmericanYearId,
                        principalTable: "AllAmericanYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllAmericanRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllAmericanYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhotoMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllAmericanRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllAmericanRecipients_AllAmericanYears_AllAmericanYearId",
                        column: x => x.AllAmericanYearId,
                        principalTable: "AllAmericanYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllAmericanRecipients_MediaAssets_PhotoMediaAssetId",
                        column: x => x.PhotoMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllAmericanYearMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllAmericanYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AltTextOverride = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CaptionOverride = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllAmericanYearMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllAmericanYearMedia_AllAmericanYears_AllAmericanYearId",
                        column: x => x.AllAmericanYearId,
                        principalTable: "AllAmericanYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllAmericanYearMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllAmericanPerformanceRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllAmericanPerformanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllAmericanRecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllAmericanPerformanceRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllAmericanPerformanceRecipients_AllAmericanPerformances_AllAmericanPerformanceId",
                        column: x => x.AllAmericanPerformanceId,
                        principalTable: "AllAmericanPerformances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllAmericanPerformanceRecipients_AllAmericanRecipients_AllAmericanRecipientId",
                        column: x => x.AllAmericanRecipientId,
                        principalTable: "AllAmericanRecipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanPerformanceRecipients_AllAmericanPerformanceId_AllAmericanRecipientId",
                table: "AllAmericanPerformanceRecipients",
                columns: new[] { "AllAmericanPerformanceId", "AllAmericanRecipientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanPerformanceRecipients_AllAmericanRecipientId",
                table: "AllAmericanPerformanceRecipients",
                column: "AllAmericanRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanPerformances_AllAmericanYearId_DisplayOrder",
                table: "AllAmericanPerformances",
                columns: new[] { "AllAmericanYearId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanRecipients_AllAmericanYearId_DisplayOrder",
                table: "AllAmericanRecipients",
                columns: new[] { "AllAmericanYearId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanRecipients_PhotoMediaAssetId",
                table: "AllAmericanRecipients",
                column: "PhotoMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYearMedia_AllAmericanYearId_DisplayOrder",
                table: "AllAmericanYearMedia",
                columns: new[] { "AllAmericanYearId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYearMedia_AllAmericanYearId_MediaAssetId",
                table: "AllAmericanYearMedia",
                columns: new[] { "AllAmericanYearId", "MediaAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYearMedia_MediaAssetId",
                table: "AllAmericanYearMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYears_HeroMediaAssetId",
                table: "AllAmericanYears",
                column: "HeroMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYears_IsPublished_DisplayOrder",
                table: "AllAmericanYears",
                columns: new[] { "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYears_Slug",
                table: "AllAmericanYears",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllAmericanYears_Year",
                table: "AllAmericanYears",
                column: "Year",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllAmericanPerformanceRecipients");

            migrationBuilder.DropTable(
                name: "AllAmericanYearMedia");

            migrationBuilder.DropTable(
                name: "AllAmericanPerformances");

            migrationBuilder.DropTable(
                name: "AllAmericanRecipients");

            migrationBuilder.DropTable(
                name: "AllAmericanYears");
        }
    }
}
