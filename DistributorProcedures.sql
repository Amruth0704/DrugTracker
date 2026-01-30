USE [DrugTracker];
GO

-- 1. Stored Procedure for Fetching Distributor Dashboard Data
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetDistributorDashboardData]
    @DistributorOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- We need batches where the latest action was a transfer TO this distributor
    -- OR batches that this distributor has already transferred elsewhere.
    -- (Mirroring the logic in GetIncomingDispatchesAsync + Manufacturer Dashboard)
    
    SELECT 
        b.DrugBatchId,
        d.DrugName,
        b.QuantityProduced,
        b.ManufactureDate,
        b.ExpiryDate,
        b.CreatedAt,
        -- Latest Action Type
        (SELECT TOP 1 h.ActionType 
         FROM [HealthCare].[BatchOwnershipHistories] h 
         WHERE h.DrugBatchId = b.DrugBatchId 
         ORDER BY h.ActionTime DESC) AS LatestAction,
        -- Latest ToOrgId
        (SELECT TOP 1 h.ToOrgId 
         FROM [HealthCare].[BatchOwnershipHistories] h 
         WHERE h.DrugBatchId = b.DrugBatchId 
         ORDER BY h.ActionTime DESC) AS LatestToOrgId,
        -- Transferred To Org Name (if transferred BY this distributor)
        (SELECT TOP 1 o.OrgName 
         FROM [HealthCare].[BatchOwnershipHistories] h
         JOIN [HealthCare].[Organizations] o ON h.ToOrgId = o.OrgId
         WHERE h.DrugBatchId = b.DrugBatchId 
           AND h.ActionType = 'TRANSFERRED' 
           AND h.FromOrgId = @DistributorOrgId
         ORDER BY h.ActionTime DESC) AS TransferredToOrgName
    FROM [HealthCare].[DrugBatches] b
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE b.DrugBatchId IN (
        SELECT DISTINCT DrugBatchId 
        FROM [HealthCare].[BatchOwnershipHistories] 
        WHERE ToOrgId = @DistributorOrgId
    )
    ORDER BY b.CreatedAt DESC;
END
GO

-- 2. Stored Procedure for Dispatching Batch to Pharmacy
CREATE OR ALTER PROCEDURE [HealthCare].[sp_DispatchBatchToPharmacy]
    @BatchId NVARCHAR(100),
    @FromOrgId INT, -- The Distributor
    @ToOrgId INT,   -- The Pharmacy
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Verification: Ensure the distributor actually holds the batch (Latest ToOrgId == @FromOrgId)
        DECLARE @LatestToOrgId INT;
        SET @LatestToOrgId = (SELECT TOP 1 ToOrgId 
                                      FROM [HealthCare].[BatchOwnershipHistories] 
                                      WHERE DrugBatchId = @BatchId 
                                      ORDER BY ActionTime DESC);
        
        DECLARE @LatestAction NVARCHAR(50);
        SET @LatestAction = (SELECT TOP 1 ActionType 
                                              FROM [HealthCare].[BatchOwnershipHistories] 
                                              WHERE DrugBatchId = @BatchId 
                                              ORDER BY ActionTime DESC);

        IF @LatestToOrgId <> @FromOrgId OR @LatestAction <> 'TRANSFERRED'
        BEGIN
            THROW 50004, 'Batch is not currently held by this organization or has already been transferred.', 1;
        END

        -- Add Ownership History Record
        INSERT INTO [HealthCare].[BatchOwnershipHistories] (DrugBatchId, ActionType, FromOrgId, ToOrgId, PerformedBy, ActionTime)
        VALUES (@BatchId, 'TRANSFERRED', @FromOrgId, @ToOrgId, @UserId, SYSDATETIME());

        -- Blockchain Recording
        DECLARE @Quantity INT;
        SET @Quantity = (SELECT QuantityProduced FROM [HealthCare].[DrugBatches] WHERE DrugBatchId = @BatchId);
        DECLARE @PrevHash NVARCHAR(64);
        SET @PrevHash = (SELECT TOP 1 CurrentHash FROM [HealthCare].[BlockchainLedgers] WHERE DrugBatchId = @BatchId ORDER BY ActionTime DESC);
        IF @PrevHash IS NULL SET @PrevHash = '0';

        DECLARE @Action NVARCHAR(50);
        SET @Action = 'TRANSFER_TO_PHARMACY';
        
        -- Use the helper function created earlier
        DECLARE @NewHash NVARCHAR(64);
        SET @NewHash = [HealthCare].[fn_ComputeBlockchainHash](@BatchId, @Action, @Quantity, @PrevHash);

        INSERT INTO [HealthCare].[BlockchainLedgers] (DrugBatchId, Action, FromOrgId, ToOrgId, Quantity, PreviousHash, CurrentHash, ActionTime)
        VALUES (@BatchId, @Action, @FromOrgId, @ToOrgId, @Quantity, @PrevHash, @NewHash, SYSDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
