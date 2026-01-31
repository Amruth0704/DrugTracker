using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTamperedToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTampered",
                schema: "HealthCare",
                table: "Inventories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTampered",
                schema: "HealthCare",
                table: "Inventories");
        }
    }
}
