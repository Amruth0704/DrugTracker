USE [DrugTracker];
GO

-- 1. Stored Procedure for Fetching Pharmacy Dashboard Data (Incoming)
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetPharmacyDashboardData]
    @PharmacyOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Fetches batches transferred TO this pharmacy that are NOT yet accepted (No RECEIVED action)
    SELECT 
        b.DrugBatchId,
        d.DrugName,
        b.QuantityProduced,
        b.ManufactureDate,
        b.ExpiryDate,
        b.CreatedAt,
        (SELECT TOP 1 h.ActionType 
         FROM [HealthCare].[BatchOwnershipHistories] h 
         WHERE h.DrugBatchId = b.DrugBatchId 
         ORDER BY h.ActionTime DESC) AS LatestAction
    FROM [HealthCare].[DrugBatches] b
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE b.DrugBatchId IN (
        SELECT DrugBatchId 
        FROM [HealthCare].[BatchOwnershipHistories] 
        WHERE ToOrgId = @PharmacyOrgId 
          AND ActionType = 'TRANSFERRED'
    )
    AND NOT EXISTS (
        SELECT 1 
        FROM [HealthCare].[BatchOwnershipHistories] h2
        WHERE h2.DrugBatchId = b.DrugBatchId 
          AND h2.ToOrgId = @PharmacyOrgId 
          AND (h2.ActionType = 'RECEIVED' OR h2.ActionType = 'ACCEPTED')
    )
    ORDER BY b.CreatedAt DESC;
END
GO

-- 2. Stored Procedure for Accepting Batch at Pharmacy
CREATE OR ALTER PROCEDURE [HealthCare].[sp_AcceptBatchAtPharmacy]
    @BatchId NVARCHAR(100),
    @PharmacyOrgId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Verification: Ensure not already accepted
        IF EXISTS (SELECT 1 FROM [HealthCare].[BatchOwnershipHistories] 
                   WHERE DrugBatchId = @BatchId AND ActionType = 'RECEIVED')
        BEGIN
            THROW 50005, 'Batch already accepted.', 1;
        END

        -- Fetch Batch Details
        DECLARE @Quantity INT;
        SET @Quantity = (SELECT QuantityProduced FROM [HealthCare].[DrugBatches] WHERE DrugBatchId = @BatchId);
        DECLARE @PrevFromOrgId INT;
        SET @PrevFromOrgId = (SELECT TOP 1 FromOrgId FROM [HealthCare].[BatchOwnershipHistories] 
                                      WHERE DrugBatchId = @BatchId AND ActionType = 'TRANSFERRED' AND ToOrgId = @PharmacyOrgId
                                      ORDER BY ActionTime DESC);

        -- 1. Add/Update Inventory
        IF EXISTS (SELECT 1 FROM [HealthCare].[Inventories] WHERE DrugBatchId = @BatchId AND PharmacyOrgId = @PharmacyOrgId)
        BEGIN
            UPDATE [HealthCare].[Inventories]
            SET AvailableQty = AvailableQty + @Quantity,
                ReceivedQty = ReceivedQty + @Quantity,
                LastUpdated = SYSDATETIME()
            WHERE DrugBatchId = @BatchId AND PharmacyOrgId = @PharmacyOrgId;
        END
        ELSE
        BEGIN
            INSERT INTO [HealthCare].[Inventories] (PharmacyOrgId, DrugBatchId, AvailableQty, ReceivedQty, LastUpdated)
            VALUES (@PharmacyOrgId, @BatchId, @Quantity, @Quantity, SYSDATETIME());
        END

        -- 2. Add Ownership History (RECEIVED)
        INSERT INTO [HealthCare].[BatchOwnershipHistories] (DrugBatchId, ActionType, FromOrgId, ToOrgId, PerformedBy, ActionTime)
        VALUES (@BatchId, 'RECEIVED', @PrevFromOrgId, @PharmacyOrgId, @UserId, SYSDATETIME());

        -- 3. Blockchain Recording
        DECLARE @PrevHash NVARCHAR(64);
        SET @PrevHash = (SELECT TOP 1 CurrentHash FROM [HealthCare].[BlockchainLedgers] WHERE DrugBatchId = @BatchId ORDER BY ActionTime DESC);
        IF @PrevHash IS NULL SET @PrevHash = '0';

        DECLARE @Action NVARCHAR(50);
        SET @Action = 'ACCEPTED_BY_PHARMACY';
        DECLARE @NewHash NVARCHAR(64);
        SET @NewHash = [HealthCare].[fn_ComputeBlockchainHash](@BatchId, @Action, @Quantity, @PrevHash);

        INSERT INTO [HealthCare].[BlockchainLedgers] (DrugBatchId, Action, ToOrgId, Quantity, PreviousHash, CurrentHash, ActionTime)
        VALUES (@BatchId, @Action, @PharmacyOrgId, @Quantity, @PrevHash, @NewHash, SYSDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 3. Stored Procedure for Fetching Pharmacy Inventory Data
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetPharmacyInventoryData]
    @PharmacyOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        i.DrugBatchId,
        d.DrugName,
        i.AvailableQty,
        i.ReceivedQty,
        b.ExpiryDate
    FROM [HealthCare].[Inventories] i
    JOIN [HealthCare].[DrugBatches] b ON i.DrugBatchId = b.DrugBatchId
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE i.PharmacyOrgId = @PharmacyOrgId
    ORDER BY b.ExpiryDate ASC;
END
GO

-- 4. Stored Procedure for Selling Drug at Pharmacy
CREATE OR ALTER PROCEDURE [HealthCare].[sp_SellDrugAtPharmacy]
    @BatchId NVARCHAR(100),
    @PharmacyOrgId INT,
    @QuantityToSell INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Verification: Ensure sufficient stock
        DECLARE @CurrentAvailable INT;
        SET @CurrentAvailable = (SELECT AvailableQty FROM [HealthCare].[Inventories] 
                                         WHERE DrugBatchId = @BatchId AND PharmacyOrgId = @PharmacyOrgId);
        
        IF @CurrentAvailable IS NULL OR @CurrentAvailable < @QuantityToSell
        BEGIN
            THROW 50006, 'Insufficient inventory.', 1;
        END

        -- 1. Update Inventory
        UPDATE [HealthCare].[Inventories]
        SET AvailableQty = AvailableQty - @QuantityToSell,
            LastUpdated = SYSDATETIME()
        WHERE DrugBatchId = @BatchId AND PharmacyOrgId = @PharmacyOrgId;

        -- 2. Blockchain Recording
        DECLARE @PrevHash NVARCHAR(64);
        SET @PrevHash = (SELECT TOP 1 CurrentHash FROM [HealthCare].[BlockchainLedgers] WHERE DrugBatchId = @BatchId ORDER BY ActionTime DESC);
        IF @PrevHash IS NULL SET @PrevHash = '0';

        DECLARE @Action NVARCHAR(50);
        SET @Action = 'SOLD_TO_CONSUMER';
        DECLARE @NewHash NVARCHAR(64);
        SET @NewHash = [HealthCare].[fn_ComputeBlockchainHash](@BatchId, @Action, @QuantityToSell, @PrevHash);

        INSERT INTO [HealthCare].[BlockchainLedgers] (DrugBatchId, Action, FromOrgId, Quantity, PreviousHash, CurrentHash, ActionTime)
        VALUES (@BatchId, @Action, @PharmacyOrgId, @QuantityToSell, @PrevHash, @NewHash, SYSDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
