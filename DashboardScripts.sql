/*
    Dashboard and CRUD Stored Procedures & Indexes
    -------------------------------------------------
    This script creates the necessary Stored Procedures and Indexes for the DrugTracker application.
    
    1. Indexes for Optimization
    2. usp_GetDistributorDashboard
    3. usp_GetPharmacyDashboard
    4. usp_DispatchBatchToPharmacy
    5. usp_AcceptBatchAtPharmacy
    6. usp_SellDrug
*/

USE [DrugTracker] -- OR whatever the DB name is, usually implied context. 
-- Ensure you are in the correct database.

--------------------------------------------------------------------------------------
-- 1. INDEXES
--------------------------------------------------------------------------------------

-- Optimize sorting by Creation Date for Batches
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DrugBatches_CreatedAt' AND object_id = OBJECT_ID('HealthCare.DrugBatches'))
BEGIN
    CREATE INDEX [IX_DrugBatches_CreatedAt] ON [HealthCare].[DrugBatches] ([CreatedAt] DESC);
END
GO

-- Optimize Dashboard filtering and 'Status' sorting (ActionType)
-- We query by ToOrgId often to find items at a location
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BatchOwnershipHistory_ToOrg_Action' AND object_id = OBJECT_ID('HealthCare.BatchOwnershipHistories'))
BEGIN
    CREATE INDEX [IX_BatchOwnershipHistory_ToOrg_Action] 
    ON [HealthCare].[BatchOwnershipHistories] ([ToOrgId], [ActionType], [ActionTime] DESC)
    INCLUDE ([DrugBatchId], [FromOrgId]);
END
GO

--------------------------------------------------------------------------------------
-- 2. DISTRIBUTOR DASHBOARD SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_GetDistributorDashboard]
    @DistributorOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    /*
       Logic: Returns batches relevant to the Distributor.
       - Incoming/Current: Last Action was 'TRANSFERRED' TO this Distributor.
       - Outgoing/History: Last Action was 'TRANSFERRED' FROM this Distributor.
       
       Sorting: 
       - RECEIVED (Actionable - currently holding) First.
       - DISPATCHED (History) Second.
       - Then by Date Recent.
    */

    WITH LatestHistory AS (
        SELECT 
            h.DrugBatchId,
            h.ActionType,
            h.ToOrgId,
            h.FromOrgId,
            h.ActionTime,
            ROW_NUMBER() OVER (PARTITION BY h.DrugBatchId ORDER BY h.ActionTime DESC) as Rn
        FROM [HealthCare].[BatchOwnershipHistories] h
    )
    SELECT 
        b.DrugBatchId,
        d.DrugName,
        b.QuantityProduced,
        b.ExpiryDate,
        b.ManufactureDate,
        b.CreatedAt,
        lh.ActionType AS LatestAction,
        lh.ActionTime,
        
        -- Derived Status for Sorting/Display
        CASE 
            WHEN lh.ToOrgId = @DistributorOrgId AND lh.ActionType = 'TRANSFERRED' THEN 'RECEIVED'
            WHEN lh.FromOrgId = @DistributorOrgId AND lh.ActionType = 'TRANSFERRED' THEN 'DISPATCHED'
            ELSE 'OTHER' 
        END AS [Status],

        -- If Dispatched, show who it went to. If Received, show who it came from (Manufacture or Other Dist).
        -- We can join Organizations to get names if needed, or return IDs.
        -- Let's return Names for the View.
        orgTo.OrgName AS PreparedFor, -- or TransferredTo
        orgFrom.OrgName AS ReceivedFrom
        
    FROM [HealthCare].[DrugBatches] b
    JOIN LatestHistory lh ON b.DrugBatchId = lh.DrugBatchId AND lh.Rn = 1
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    LEFT JOIN [HealthCare].[Organizations] orgTo ON lh.ToOrgId = orgTo.OrgId
    LEFT JOIN [HealthCare].[Organizations] orgFrom ON lh.FromOrgId = orgFrom.OrgId
    WHERE 
        (lh.ToOrgId = @DistributorOrgId AND lh.ActionType = 'TRANSFERRED') -- Currently Here
        OR 
        (lh.FromOrgId = @DistributorOrgId AND lh.ActionType = 'TRANSFERRED') -- Was Here, Sent Out
    ORDER BY 
        CASE 
            WHEN (lh.ToOrgId = @DistributorOrgId AND lh.ActionType = 'TRANSFERRED') THEN 1 -- Received/Actionable
            ELSE 2 -- Dispatched
        END,
        lh.ActionTime DESC;
END
GO

--------------------------------------------------------------------------------------
-- 3. PHARMACY DASHBOARD SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_GetPharmacyDashboard]
    @PharmacyOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    /*
       Logic: Returns batches incoming to Pharmacy.
       - Pending Acceptance: Last Action was 'TRANSFERRED' TO this Pharmacy.
       - Accepted: Last Action was 'RECEIVED' or 'ACCEPTED' BY this Pharmacy.
       
       Sorting: 
       - PENDING (Actionable) First.
       - ACCEPTED (History) Second.
    */

    WITH LatestHistory AS (
        SELECT 
            h.DrugBatchId,
            h.ActionType,
            h.ToOrgId,
            h.FromOrgId,
            h.ActionTime,
            ROW_NUMBER() OVER (PARTITION BY h.DrugBatchId ORDER BY h.ActionTime DESC) as Rn
        FROM [HealthCare].[BatchOwnershipHistories] h
    )
    SELECT 
        b.DrugBatchId,
        d.DrugName,
        b.QuantityProduced,
        b.ExpiryDate,
        b.ManufactureDate,
        lh.ActionType AS LatestAction,
        lh.ActionTime,
        
        CASE 
            WHEN lh.ToOrgId = @PharmacyOrgId AND lh.ActionType = 'TRANSFERRED' THEN 'PENDING'
            WHEN lh.ToOrgId = @PharmacyOrgId AND (lh.ActionType = 'RECEIVED' OR lh.ActionType = 'ACCEPTED') THEN 'ACCEPTED'
            ELSE 'OTHER' 
        END AS [Status]

    FROM [HealthCare].[DrugBatches] b
    JOIN LatestHistory lh ON b.DrugBatchId = lh.DrugBatchId AND lh.Rn = 1
    JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE 
        lh.ToOrgId = @PharmacyOrgId 
        AND (lh.ActionType IN ('TRANSFERRED', 'RECEIVED', 'ACCEPTED'))
    ORDER BY 
        CASE 
            WHEN lh.ActionType = 'TRANSFERRED' THEN 1 -- Pending
            ELSE 2 -- Accepted
        END,
        lh.ActionTime DESC;
END
GO

--------------------------------------------------------------------------------------
-- 4. DISPATCH BATCH TO PHARMACY SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_DispatchBatchToPharmacy]
    @BatchId NVARCHAR(50),
    @FromDistributorId INT,
    @ToPharmacyId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Validation: Check if batch is currently held by Distributor
    DECLARE @LatestAction VARCHAR(50);
    DECLARE @CurrentHolder INT;
    
    SELECT TOP 1 
        @LatestAction = ActionType, 
        @CurrentHolder = ToOrgId
    FROM [HealthCare].[BatchOwnershipHistories]
    WHERE DrugBatchId = @BatchId
    ORDER BY ActionTime DESC;

    IF @CurrentHolder <> @FromDistributorId OR @LatestAction <> 'TRANSFERRED'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51000, 'Batch is not currently held by this Distributor or has already been dispatched.', 1;
    END

    -- Insert History Record
    INSERT INTO [HealthCare].[BatchOwnershipHistories]
        (DrugBatchId, ActionType, FromOrgId, ToOrgId, PerformedBy, ActionTime)
    VALUES
        (@BatchId, 'TRANSFERRED', @FromDistributorId, @ToPharmacyId, @UserId, SYSDATETIME());

    -- Blockchain Ledger Entry (Simulated as Table Insert)
    INSERT INTO [HealthCare].[BlockchainLedgers]
        (DrugBatchId, Action, FromOrgId, ToOrgId, ActionTime)
    VALUES
        (@BatchId, 'TRANSFER_TO_PHARMACY', @FromDistributorId, @ToPharmacyId, SYSDATETIME());

    COMMIT TRANSACTION;
END
GO

--------------------------------------------------------------------------------------
-- 5. ACCEPT BATCH AT PHARMACY SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_AcceptBatchAtPharmacy]
    @BatchId NVARCHAR(50),
    @PharmacyOrgId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Validation
    DECLARE @LatestAction VARCHAR(50);
    DECLARE @CurrentToOrg INT;
    DECLARE @FromDistId INT;
    
    SELECT TOP 1 
        @LatestAction = ActionType, 
        @CurrentToOrg = ToOrgId,
        @FromDistId = FromOrgId
    FROM [HealthCare].[BatchOwnershipHistories]
    WHERE DrugBatchId = @BatchId
    ORDER BY ActionTime DESC;

    IF @CurrentToOrg <> @PharmacyOrgId OR @LatestAction <> 'TRANSFERRED'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51001, 'Batch is not pending acceptance at this Pharmacy.', 1;
    END

    -- Get Batch Details for Inventory
    DECLARE @Quantity INT;
    DECLARE @DrugId INT;
    
    SELECT @Quantity = QuantityProduced, @DrugId = DrugId 
    FROM [HealthCare].[DrugBatches] WHERE DrugBatchId = @BatchId;

    -- Insert/Update Inventory
    -- Assuming Inventory is per Batch or Per Drug? Current Code implies Per Batch logic in some places, 
    -- but table definition usually aggregates. 
    -- Looking at Repository code: `GetPharmacyInventoryAsync` returns list of Batches.
    -- Table `Inventories` likely has `DrugBatchId` column from previous `InventoryRepository.cs` snippet?
    -- Snippet showed: `i.DrugBatchId`, `i.AvailableQty`. So it is per Batch.
    
    MERGE [HealthCare].[Inventories] AS target
    USING (SELECT @PharmacyOrgId AS PharmacyOrgId, @BatchId AS DrugBatchId) AS source
    ON (target.PharmacyOrgId = source.PharmacyOrgId AND target.DrugBatchId = source.DrugBatchId)
    WHEN MATCHED THEN
        UPDATE SET 
            AvailableQty = target.AvailableQty, -- Should strictly be just adding? But if it's per batch, it's unique.
            ReceivedQty = target.ReceivedQty, -- If per batch, it's set once.
            LastUpdated = SYSDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (PharmacyOrgId, DrugBatchId, AvailableQty, ReceivedQty, LastUpdated)
        VALUES (@PharmacyOrgId, @BatchId, @Quantity, @Quantity, SYSDATETIME());

    -- History
    INSERT INTO [HealthCare].[BatchOwnershipHistories]
        (DrugBatchId, ActionType, FromOrgId, ToOrgId, PerformedBy, ActionTime)
    VALUES
        (@BatchId, 'RECEIVED', @FromDistId, @PharmacyOrgId, @UserId, SYSDATETIME());

    -- Blockchain
    INSERT INTO [HealthCare].[BlockchainLedgers]
        (DrugBatchId, Action, ToOrgId, Quantity, ActionTime)
    VALUES
        (@BatchId, 'ACCEPTED_BY_PHARMACY', @PharmacyOrgId, @Quantity, SYSDATETIME());

    COMMIT TRANSACTION;
END
GO

--------------------------------------------------------------------------------------
-- 6. SELL DRUG SP
--------------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [HealthCare].[usp_SellDrug]
    @BatchId NVARCHAR(50),
    @PharmacyOrgId INT,
    @QuantityToSell INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @CurrentQty INT;
    
    SELECT @CurrentQty = AvailableQty 
    FROM [HealthCare].[Inventories] 
    WHERE PharmacyOrgId = @PharmacyOrgId AND DrugBatchId = @BatchId;

    IF @CurrentQty IS NULL OR @CurrentQty < @QuantityToSell
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51002, 'Insufficient inventory.', 1;
    END

    -- Update Inventory
    UPDATE [HealthCare].[Inventories]
    SET AvailableQty = AvailableQty - @QuantityToSell,
        LastUpdated = SYSDATETIME()
    WHERE PharmacyOrgId = @PharmacyOrgId AND DrugBatchId = @BatchId;

    -- Blockchain (Sale Record)
    INSERT INTO [HealthCare].[BlockchainLedgers]
        (DrugBatchId, Action, FromOrgId, Quantity, ActionTime)
    VALUES
        (@BatchId, 'SOLD_TO_CONSUMER', @PharmacyOrgId, @QuantityToSell, SYSDATETIME());

    COMMIT TRANSACTION;
END
GO
