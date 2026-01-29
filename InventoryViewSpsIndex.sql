USE [DrugTracker];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*========================================================
  INDEXES (critical for inventory lookups & updates)
========================================================*/

/* Inventories: composite key access (Pharmacy + Batch) */
CREATE NONCLUSTERED INDEX IX_Inventories_Pharmacy_Batch
ON [HealthCare].[Inventories] (PharmacyOrgId, DrugBatchId)
INCLUDE (AvailableQty, LastUpdated);
GO

/* Inventories: pharmacy stock listing */
CREATE NONCLUSTERED INDEX IX_Inventories_Pharmacy_AvailableQty
ON [HealthCare].[Inventories] (PharmacyOrgId, AvailableQty)
INCLUDE (DrugBatchId);
GO

/* DrugBatches join support */
CREATE NONCLUSTERED INDEX IX_DrugBatches_DrugBatchId_DrugId
ON [HealthCare].[DrugBatches] (DrugBatchId, DrugId);
GO

/*========================================================
  VIEWS (READ OPERATIONS)
========================================================*/

/* Inventory item with batch + drug details */
CREATE OR ALTER VIEW [HealthCare].[vw_InventoryItem_Details]
AS
SELECT
    i.DrugBatchId,
    i.PharmacyOrgId,
    i.AvailableQty,
    i.ReceivedQty,
    i.LastUpdated,

    b.DrugId,
    b.ManufactureDate,
    b.ExpiryDate,

    d.DrugName,
    d.DrugCode
FROM [HealthCare].[Inventories] i
INNER JOIN [HealthCare].[DrugBatches] b
    ON i.DrugBatchId = b.DrugBatchId
INNER JOIN [HealthCare].[Drugs] d
    ON b.DrugId = d.DrugId;
GO

/* Pharmacy inventory (only available stock) */
CREATE OR ALTER VIEW [HealthCare].[vw_PharmacyInventory]
AS
SELECT
    i.PharmacyOrgId,
    i.DrugBatchId,
    i.AvailableQty,
	i.ReceivedQty, 
    i.LastUpdated,

    d.DrugId,
    d.DrugName,
    d.DrugCode,

    b.ExpiryDate
FROM [HealthCare].[Inventories] i
INNER JOIN [HealthCare].[DrugBatches] b
    ON i.DrugBatchId = b.DrugBatchId
INNER JOIN [HealthCare].[Drugs] d
    ON b.DrugId = d.DrugId
WHERE i.AvailableQty > 0;
GO

/*========================================================
  STORED PROCEDURES (WRITE / UPSERT LOGIC)
========================================================*/

/* Add or Update Inventory (UPSERT) */
CREATE OR ALTER PROCEDURE [HealthCare].[sp_AddOrUpdateInventory]
    @DrugBatchId NVARCHAR(100),
    @PharmacyOrgId INT,
    @AvailableQty INT,
    @ReceivedQty INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM [HealthCare].[Inventories]
        WHERE DrugBatchId = @DrugBatchId
          AND PharmacyOrgId = @PharmacyOrgId
    )
    BEGIN
        UPDATE [HealthCare].[Inventories]
        SET
            AvailableQty = @AvailableQty,
            ReceivedQty = @ReceivedQty,
            LastUpdated = SYSDATETIME()
        WHERE DrugBatchId = @DrugBatchId
          AND PharmacyOrgId = @PharmacyOrgId;
    END
    ELSE
    BEGIN
        INSERT INTO [HealthCare].[Inventories]
        (
            DrugBatchId,
            PharmacyOrgId,
            AvailableQty,
            ReceivedQty,
            LastUpdated
        )
        VALUES
        (
            @DrugBatchId,
            @PharmacyOrgId,
            @AvailableQty,
            @ReceivedQty,
            SYSDATETIME()
        );
    END
END;
GO
