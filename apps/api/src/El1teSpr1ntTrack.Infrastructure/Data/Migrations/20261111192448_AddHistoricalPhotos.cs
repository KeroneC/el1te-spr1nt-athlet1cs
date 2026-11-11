using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ByteLength = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    ImportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CapturedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Review = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyPhotos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegacyPhotoSources",
                columns: table => new
                {
                    UrlSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    LegacyPhotoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyPhotoSources", x => x.UrlSha256);
                    table.ForeignKey(
                        name: "FK_LegacyPhotoSources_LegacyPhotos_LegacyPhotoId",
                        column: x => x.LegacyPhotoId,
                        principalTable: "LegacyPhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegacyPhotoPages",
                columns: table => new
                {
                    SourceUrlSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PageUrlSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Collection = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyPhotoPages", x => new { x.SourceUrlSha256, x.PageUrlSha256 });
                    table.ForeignKey(
                        name: "FK_LegacyPhotoPages_LegacyPhotoSources_SourceUrlSha256",
                        column: x => x.SourceUrlSha256,
                        principalTable: "LegacyPhotoSources",
                        principalColumn: "UrlSha256",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPhotoPages_Collection",
                table: "LegacyPhotoPages",
                column: "Collection");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPhotos_Review_Id",
                table: "LegacyPhotos",
                columns: new[] { "Review", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPhotos_Sha256",
                table: "LegacyPhotos",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPhotoSources_LegacyPhotoId",
                table: "LegacyPhotoSources",
                column: "LegacyPhotoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyPhotoPages");

            migrationBuilder.DropTable(
                name: "LegacyPhotoSources");

            migrationBuilder.DropTable(
                name: "LegacyPhotos");
        }
    }
}
