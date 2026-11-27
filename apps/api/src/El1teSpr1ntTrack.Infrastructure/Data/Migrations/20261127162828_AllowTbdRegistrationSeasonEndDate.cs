using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowTbdRegistrationSeasonEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "RegistrationSeasons",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [dbo].[RegistrationSeasons] WHERE [EndDate] IS NULL)
                    THROW 51000, 'Cannot downgrade while a registration season has a TBD end date.', 1;
                """);
            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "RegistrationSeasons",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
