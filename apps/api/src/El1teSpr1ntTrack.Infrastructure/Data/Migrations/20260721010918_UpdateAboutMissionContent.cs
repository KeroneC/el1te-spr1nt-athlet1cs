using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAboutMissionContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                columns: new[] { "Body", "Title" },
                values: new object[] { "El1te Spr1nt Athlet1cs offers a track and field developmental program that includes preseason strength and conditioning workouts, with the goal of participating in track meets held in the spring and summer each year. We compete locally, regionally, and nationally in track and field events sanctioned by USATF and AAU. Our club is a nonprofit organization formed with the mission of promoting track and field for youth ages 7 to 18 in our local area. By doing so, we provide an avenue for each athlete to enhance their talent while achieving whatever life goals they may have set for themselves. This program is not a recreational program; rather, it is designed to empower young competitive athletes by teaching basic running skills, body mechanics, event fundamentals, sportsmanship, and discipline.", "Our Mission" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                columns: new[] { "Body", "Title" },
                values: new object[] { "El1te Spr1nt Athlet1cs was built to make quality track and field opportunities accessible to young athletes and their families.", "Our Story" });
        }
    }
}
