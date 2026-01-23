using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                schema: "HealthCare",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                schema: "HealthCare",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                schema: "HealthCare",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                schema: "HealthCare",
                table: "Users");
        }
    }
}
