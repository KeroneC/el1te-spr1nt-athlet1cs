using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullNewAthleteApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "RegistrationSubmissions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApplicationVersion",
                table: "RegistrationSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AthleteLastName",
                table: "RegistrationSubmissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "RegistrationSubmissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "RegistrationSubmissions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "RegistrationSubmissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "RegistrationSubmissions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                table: "RegistrationSubmissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "RegistrationSubmissions",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "RegistrationSubmissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolDistrict",
                table: "RegistrationSubmissions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolGrade",
                table: "RegistrationSubmissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryGuardianEmail",
                table: "RegistrationSubmissions",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryGuardianName",
                table: "RegistrationSubmissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryGuardianPhone",
                table: "RegistrationSubmissions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "RegistrationSubmissions",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "RegistrationSubmissions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FirstPracticeDate",
                table: "RegistrationSeasons",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "ApplicationVersion",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "AthleteLastName",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "City",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "SchoolDistrict",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "SchoolGrade",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "SecondaryGuardianEmail",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "SecondaryGuardianName",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "SecondaryGuardianPhone",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "State",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "FirstPracticeDate",
                table: "RegistrationSeasons");
        }
    }
}
