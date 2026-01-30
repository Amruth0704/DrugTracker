using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugBatches_Drugs_DrugId",
                schema: "HealthCare",
                table: "DrugBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrgId",
                schema: "HealthCare",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ActivityLogs",
                schema: "HealthCare");

            migrationBuilder.AddColumn<string>(
                name: "CurrentHash",
                schema: "HealthCare",
                table: "BlockchainLedgers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                schema: "HealthCare",
                table: "BlockchainLedgers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DistributorDashboardData",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugBatchId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityProduced = table.Column<int>(type: "int", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LatestToOrgId = table.Column<int>(type: "int", nullable: false),
                    TransferredToOrgName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ManufacturerDashboardData",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugBatchId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityProduced = table.Column<int>(type: "int", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransferredToOrgName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "PharmacyDashboardData",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugBatchId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityProduced = table.Column<int>(type: "int", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestAction = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "PharmacyInventoryData",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugBatchId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvailableQty = table.Column<int>(type: "int", nullable: false),
                    ReceivedQty = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.AddForeignKey(
                name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "DrugBatchId",
                principalSchema: "HealthCare",
                principalTable: "DrugBatches",
                principalColumn: "DrugBatchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugBatches_Drugs_DrugId",
                schema: "HealthCare",
                table: "DrugBatches",
                column: "DrugId",
                principalSchema: "HealthCare",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "Inventories",
                column: "DrugBatchId",
                principalSchema: "HealthCare",
                principalTable: "DrugBatches",
                principalColumn: "DrugBatchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrgId",
                schema: "HealthCare",
                table: "Users",
                column: "OrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugBatches_Drugs_DrugId",
                schema: "HealthCare",
                table: "DrugBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrgId",
                schema: "HealthCare",
                table: "Users");

            migrationBuilder.DropTable(
                name: "DistributorDashboardData",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "ManufacturerDashboardData",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "PharmacyDashboardData",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "PharmacyInventoryData",
                schema: "HealthCare");

            migrationBuilder.DropColumn(
                name: "CurrentHash",
                schema: "HealthCare",
                table: "BlockchainLedgers");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                schema: "HealthCare",
                table: "BlockchainLedgers");

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                schema: "HealthCare",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "HealthCare",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                schema: "HealthCare",
                table: "ActivityLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "DrugBatchId",
                principalSchema: "HealthCare",
                principalTable: "DrugBatches",
                principalColumn: "DrugBatchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugBatches_Drugs_DrugId",
                schema: "HealthCare",
                table: "DrugBatches",
                column: "DrugId",
                principalSchema: "HealthCare",
                principalTable: "Drugs",
                principalColumn: "DrugId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "Inventories",
                column: "DrugBatchId",
                principalSchema: "HealthCare",
                principalTable: "DrugBatches",
                principalColumn: "DrugBatchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrgId",
                schema: "HealthCare",
                table: "Users",
                column: "OrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
