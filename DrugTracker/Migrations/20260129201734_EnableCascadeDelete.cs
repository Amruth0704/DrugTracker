using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class EnableCascadeDelete : Migration
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
                name: "FK_DrugBatches_Organizations_CreatedByOrgId",
                schema: "HealthCare",
                table: "DrugBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Organizations_PharmacyOrgId",
                schema: "HealthCare",
                table: "Inventories");

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
                name: "FK_DrugBatches_Organizations_CreatedByOrgId",
                schema: "HealthCare",
                table: "DrugBatches",
                column: "CreatedByOrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Organizations_PharmacyOrgId",
                schema: "HealthCare",
                table: "Inventories",
                column: "PharmacyOrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
                onDelete: ReferentialAction.Cascade);
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
                name: "FK_DrugBatches_Organizations_CreatedByOrgId",
                schema: "HealthCare",
                table: "DrugBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Organizations_PharmacyOrgId",
                schema: "HealthCare",
                table: "Inventories");

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
                name: "FK_DrugBatches_Organizations_CreatedByOrgId",
                schema: "HealthCare",
                table: "DrugBatches",
                column: "CreatedByOrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Organizations_PharmacyOrgId",
                schema: "HealthCare",
                table: "Inventories",
                column: "PharmacyOrgId",
                principalSchema: "HealthCare",
                principalTable: "Organizations",
                principalColumn: "OrgId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
