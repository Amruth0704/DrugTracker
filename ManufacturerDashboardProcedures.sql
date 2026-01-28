USE [DrugTracker];
GO

CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetManufacturerDashboardData]
    @ManufacturerOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        b.DrugBatchId,
        d.DrugName,
        b.QuantityProduced,
        b.ManufactureDate,
        b.ExpiryDate,
        b.CreatedAt,
        -- Get Latest Action for this batch
        (SELECT TOP 1 h.ActionType 
         FROM [HealthCare].[BatchOwnershipHistories] h 
         WHERE h.DrugBatchId = b.DrugBatchId 
         ORDER BY h.ActionTime DESC) AS LatestAction,
        -- Get the name of the organization it was transferred to (if any)
        (SELECT TOP 1 o.OrgName 
         FROM [HealthCare].[BatchOwnershipHistories] h
         JOIN [HealthCare].[Organizations] o ON h.ToOrgId = o.OrgId
         WHERE h.DrugBatchId = b.DrugBatchId 
           AND h.ActionType = 'TRANSFERRED' 
           AND h.FromOrgId = @ManufacturerOrgId
         ORDER BY h.ActionTime DESC) AS TransferredToOrgName
    FROM [HealthCare].[DrugBatches] b
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE b.CreatedByOrgId = @ManufacturerOrgId
    ORDER BY b.CreatedAt DESC;
END
GO
