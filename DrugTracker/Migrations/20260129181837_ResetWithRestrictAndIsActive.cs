using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class ResetWithRestrictAndIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchOwnershipHistories_Organizations_FromOrgId",
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

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "HealthCare",
                table: "Drugs",
                type: "bit",
                nullable: false,
                defaultValue: true);

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
                name: "FK_BatchOwnershipHistories_Organizations_FromOrgId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "FromOrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchOwnershipHistories_Organizations_FromOrgId",
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

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "HealthCare",
                table: "Drugs");

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
                name: "FK_BatchOwnershipHistories_Organizations_FromOrgId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "FromOrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId");

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
        }
    }
}
