using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "HealthCare");

            // Manually creating Ledger Table with Append-Only feature
            migrationBuilder.Sql(
                @"CREATE TABLE [HealthCare].[BlockchainLedgers] (
                    [LedgerId] int NOT NULL IDENTITY,
                    [DrugBatchId] nvarchar(100) NOT NULL,
                    [Action] nvarchar(50) NOT NULL,
                    [FromOrgId] int NULL,
                    [ToOrgId] int NULL,
                    [Quantity] int NULL,
                    [ActionTime] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
                    CONSTRAINT [PK_BlockchainLedgers] PRIMARY KEY ([LedgerId])
                  )
                  WITH (LEDGER = ON (APPEND_ONLY = ON));");

            migrationBuilder.CreateTable(
                name: "Drugs",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.DrugId);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "HealthCare",
                columns: table => new
                {
                    OrgId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrgType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.OrgId);
                });

            migrationBuilder.CreateTable(
                name: "DrugBatches",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugBatchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    QuantityProduced = table.Column<int>(type: "int", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByOrgId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugBatches", x => x.DrugBatchId);
                    table.ForeignKey(
                        name: "FK_DrugBatches_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "HealthCare",
                        principalTable: "Drugs",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugBatches_Organizations_CreatedByOrgId",
                        column: x => x.CreatedByOrgId,
                        principalSchema: "HealthCare",
                        principalTable: "Organizations",
                        principalColumn: "OrgId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "HealthCare",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrgId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrgId",
                        column: x => x.OrgId,
                        principalSchema: "HealthCare",
                        principalTable: "Organizations",
                        principalColumn: "OrgId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                schema: "HealthCare",
                columns: table => new
                {
                    DrugBatchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PharmacyOrgId = table.Column<int>(type: "int", nullable: false),
                    AvailableQty = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.DrugBatchId);
                    table.ForeignKey(
                        name: "FK_Inventories_DrugBatches_DrugBatchId",
                        column: x => x.DrugBatchId,
                        principalSchema: "HealthCare",
                        principalTable: "DrugBatches",
                        principalColumn: "DrugBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_Organizations_PharmacyOrgId",
                        column: x => x.PharmacyOrgId,
                        principalSchema: "HealthCare",
                        principalTable: "Organizations",
                        principalColumn: "OrgId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                schema: "HealthCare",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
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

            migrationBuilder.CreateTable(
                name: "BatchOwnershipHistories",
                schema: "HealthCare",
                columns: table => new
                {
                    OwnershipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugBatchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromOrgId = table.Column<int>(type: "int", nullable: true),
                    ToOrgId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PerformedBy = table.Column<int>(type: "int", nullable: false),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchOwnershipHistories", x => x.OwnershipId);
                    table.ForeignKey(
                        name: "FK_BatchOwnershipHistories_DrugBatches_DrugBatchId",
                        column: x => x.DrugBatchId,
                        principalSchema: "HealthCare",
                        principalTable: "DrugBatches",
                        principalColumn: "DrugBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchOwnershipHistories_Organizations_FromOrgId",
                        column: x => x.FromOrgId,
                        principalSchema: "HealthCare",
                        principalTable: "Organizations",
                        principalColumn: "OrgId");
                    table.ForeignKey(
                        name: "FK_BatchOwnershipHistories_Organizations_ToOrgId",
                        column: x => x.ToOrgId,
                        principalSchema: "HealthCare",
                        principalTable: "Organizations",
                        principalColumn: "OrgId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchOwnershipHistories_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalSchema: "HealthCare",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                schema: "HealthCare",
                table: "ActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchOwnershipHistories_DrugBatchId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "DrugBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchOwnershipHistories_FromOrgId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "FromOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchOwnershipHistories_PerformedBy",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BatchOwnershipHistories_ToOrgId",
                schema: "HealthCare",
                table: "BatchOwnershipHistories",
                column: "ToOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugBatches_CreatedByOrgId",
                schema: "HealthCare",
                table: "DrugBatches",
                column: "CreatedByOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugBatches_DrugId",
                schema: "HealthCare",
                table: "DrugBatches",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_DrugCode",
                schema: "HealthCare",
                table: "Drugs",
                column: "DrugCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_PharmacyOrgId",
                schema: "HealthCare",
                table: "Inventories",
                column: "PharmacyOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrgId",
                schema: "HealthCare",
                table: "Users",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                schema: "HealthCare",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "BatchOwnershipHistories",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "BlockchainLedgers",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "Inventories",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "DrugBatches",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "Drugs",
                schema: "HealthCare");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "HealthCare");
        }
    }
}
