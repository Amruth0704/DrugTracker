using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedQtyToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReceivedQty",
                schema: "HealthCare",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedQty",
                schema: "HealthCare",
                table: "Inventories");
        }
    }
}
