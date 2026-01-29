USE [DrugTracker]
GO

--------------------------------------------------------------------------------------
-- 7. GET PHARMACY INVENTORY SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_GetPharmacyInventory]
    @PharmacyOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        i.PharmacyOrgId,
        i.DrugBatchId,
        i.AvailableQty,
        i.ReceivedQty,
        (i.ReceivedQty - i.AvailableQty) AS QuantitySold,
        i.LastUpdated,
        -- Details from Joins as DTO properties
        b.ExpiryDate,
        d.DrugName
    FROM [HealthCare].[Inventories] i
    JOIN [HealthCare].[DrugBatches] b ON i.DrugBatchId = b.DrugBatchId
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE i.PharmacyOrgId = @PharmacyOrgId AND i.AvailableQty > 0;
END
GO

--------------------------------------------------------------------------------------
-- 8. GET INVENTORY ITEM SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_GetInventoryItem]
    @PharmacyOrgId INT,
    @BatchId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        i.PharmacyOrgId,
        i.DrugBatchId,
        i.AvailableQty,
        i.ReceivedQty,
        i.LastUpdated
    FROM [HealthCare].[Inventories] i
    WHERE i.PharmacyOrgId = @PharmacyOrgId AND i.DrugBatchId = @BatchId;
END
GO
