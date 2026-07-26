using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreCatalogAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SquareCatalogObjectId",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SquareCatalogVersion",
                table: "ProductVariants",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SquareCatalogObjectId",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SquareCatalogVersion",
                table: "Products",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductOptionValues",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SquareCatalogObjectId",
                table: "ProductOptionValues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductOptions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SquareCatalogObjectId",
                table: "ProductOptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductModifierGroups",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SquareCatalogObjectId",
                table: "ProductCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryStocktakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VariantCount = table.Column<int>(type: "int", nullable: false),
                    ChangedVariantCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStocktakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryStocktakes_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SquareCatalogImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProductsDiscovered = table.Column<int>(type: "int", nullable: false),
                    ProductsCreated = table.Column<int>(type: "int", nullable: false),
                    ProductsSkipped = table.Column<int>(type: "int", nullable: false),
                    ImagesImported = table.Column<int>(type: "int", nullable: false),
                    SafeFailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SquareCatalogImportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SquareCatalogImportRuns_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryStocktakeLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryStocktakeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedOnHandQuantity = table.Column<int>(type: "int", nullable: false),
                    CountedOnHandQuantity = table.Column<int>(type: "int", nullable: false),
                    InventoryAdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStocktakeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryStocktakeLines_InventoryAdjustments_InventoryAdjustmentId",
                        column: x => x.InventoryAdjustmentId,
                        principalTable: "InventoryAdjustments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryStocktakeLines_InventoryStocktakes_InventoryStocktakeId",
                        column: x => x.InventoryStocktakeId,
                        principalTable: "InventoryStocktakes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryStocktakeLines_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SquareCatalogObjectId",
                table: "ProductVariants",
                column: "SquareCatalogObjectId",
                unique: true,
                filter: "[SquareCatalogObjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SquareCatalogObjectId",
                table: "Products",
                column: "SquareCatalogObjectId",
                unique: true,
                filter: "[SquareCatalogObjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_SquareCatalogObjectId",
                table: "ProductCategories",
                column: "SquareCatalogObjectId",
                unique: true,
                filter: "[SquareCatalogObjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocktakeLines_InventoryAdjustmentId",
                table: "InventoryStocktakeLines",
                column: "InventoryAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocktakeLines_InventoryStocktakeId_ProductVariantId",
                table: "InventoryStocktakeLines",
                columns: new[] { "InventoryStocktakeId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocktakeLines_ProductVariantId",
                table: "InventoryStocktakeLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocktakes_ActorUserId",
                table: "InventoryStocktakes",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocktakes_CreatedAt",
                table: "InventoryStocktakes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SquareCatalogImportRuns_ActorUserId",
                table: "SquareCatalogImportRuns",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SquareCatalogImportRuns_CreatedAt",
                table: "SquareCatalogImportRuns",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryStocktakeLines");

            migrationBuilder.DropTable(
                name: "SquareCatalogImportRuns");

            migrationBuilder.DropTable(
                name: "InventoryStocktakes");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_SquareCatalogObjectId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_SquareCatalogObjectId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_SquareCatalogObjectId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "SquareCatalogObjectId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SquareCatalogVersion",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SquareCatalogObjectId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SquareCatalogVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "SquareCatalogObjectId",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "SquareCatalogObjectId",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductModifierGroups");

            migrationBuilder.DropColumn(
                name: "SquareCatalogObjectId",
                table: "ProductCategories");
        }
    }
}
