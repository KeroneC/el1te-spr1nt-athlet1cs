using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHallOfFameCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HallOfFameInductees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Affiliation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhotoAlt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InductionYear = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallOfFameInductees", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HallOfFameInductees",
                columns: new[] { "Id", "Affiliation", "CreatedAtUtc", "DisplayOrder", "InductionYear", "IsActive", "Name", "PhotoAlt", "PhotoUrl", "Slug", "Summary", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("58000000-0000-0000-0000-000000000001"), "Penn State University", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, null, true, "Dani Prunzik", "Dani Prunzik holding an American flag in her Penn State track uniform", "/images/hall-of-fame/dani-prunzik.jpeg", "dani-prunzik", "Upper St. Clair High School class of 2023 graduate, Penn State student, and talented sprinter with a 60m indoor PR of 7.57.", null },
                    { new Guid("58000000-0000-0000-0000-000000000002"), "Youngstown State University", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, null, true, "Kaitlyn Eger", "Kaitlyn Eger posing with a pole vault pole in her Youngstown State uniform", "/images/hall-of-fame/kaitlyn-eger.jpg", "kaitlyn-eger", "Youngstown State University student-athlete studying Exercise Science (Pre-PT). A multi-time top-5 Horizon League finisher and Meet MVP who helped lead back-to-back conference championships in 2024 and 2025.", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HallOfFameInductees_DisplayOrder",
                table: "HallOfFameInductees",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_HallOfFameInductees_InductionYear",
                table: "HallOfFameInductees",
                column: "InductionYear");

            migrationBuilder.CreateIndex(
                name: "IX_HallOfFameInductees_Slug",
                table: "HallOfFameInductees",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HallOfFameInductees");
        }
    }
}
