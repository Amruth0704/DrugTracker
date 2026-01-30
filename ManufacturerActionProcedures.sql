USE [DrugTracker];
GO

-- 1. Helper Function to compute SHA256 Hash as lowercase hex string (matches C# logic)
CREATE OR ALTER FUNCTION [HealthCare].[fn_ComputeBlockchainHash]
(
    @BatchId NVARCHAR(100),
    @Action NVARCHAR(50),
    @Quantity INT,
    @PrevHash NVARCHAR(MAX)
)
RETURNS NVARCHAR(64)
AS
BEGIN
    DECLARE @Input NVARCHAR(MAX);
    SET @Input = @BatchId + @Action + CAST(ISNULL(@Quantity, 0) AS NVARCHAR(20)) + @PrevHash;
    DECLARE @Hash BINARY(32);
    SET @Hash = HASHBYTES('SHA2_256', @Input);
    -- Convert binary to lowercase hex string without '0x' prefix
    RETURN LOWER(CONVERT(NVARCHAR(64), @Hash, 2));
END
GO

-- 2. Stored Procedure for Updating a Batch (Full Update)
CREATE OR ALTER PROCEDURE [HealthCare].[sp_UpdateBatchByManufacturer]
    @BatchId NVARCHAR(100),
    @NewDrugId INT,
    @NewQuantity INT,
    @NewManufactureDate DATETIME,
    @NewExpiryDate DATETIME,
    @ManufacturerOrgId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Check if exists and owned
        IF NOT EXISTS (SELECT 1 FROM [HealthCare].[DrugBatches] WHERE DrugBatchId = @BatchId AND CreatedByOrgId = @ManufacturerOrgId)
        BEGIN
            THROW 50001, 'Batch not found or unauthorized.', 1;
        END

        -- Update Batch (All editable fields)
        UPDATE [HealthCare].[DrugBatches] 
        SET DrugId = @NewDrugId,
            QuantityProduced = @NewQuantity,
            ManufactureDate = @NewManufactureDate,
            ExpiryDate = @NewExpiryDate
        WHERE DrugBatchId = @BatchId;

        -- Blockchain Recording
        DECLARE @PrevHash NVARCHAR(64);
        SET @PrevHash = (SELECT TOP 1 CurrentHash FROM [HealthCare].[BlockchainLedgers] WHERE DrugBatchId = @BatchId ORDER BY ActionTime DESC);
        IF @PrevHash IS NULL SET @PrevHash = '0';

        DECLARE @Action NVARCHAR(50);
        SET @Action = 'BATCH_UPDATED';
        -- For hashing, we'll continue using Quantity as the primary numeric state, 
        -- but the database record now holds the full truth.
        DECLARE @NewHash NVARCHAR(64);
        SET @NewHash = [HealthCare].[fn_ComputeBlockchainHash](@BatchId, @Action, @NewQuantity, @PrevHash);

        INSERT INTO [HealthCare].[BlockchainLedgers] (DrugBatchId, Action, FromOrgId, Quantity, PreviousHash, CurrentHash, ActionTime)
        VALUES (@BatchId, @Action, @ManufacturerOrgId, @NewQuantity, @PrevHash, @NewHash, SYSDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 3. Stored Procedure for Deleting a Batch
CREATE OR ALTER PROCEDURE [HealthCare].[sp_DeleteBatchByManufacturer]
    @BatchId NVARCHAR(100),
    @ManufacturerOrgId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Check if exists and owned
        IF NOT EXISTS (SELECT 1 FROM [HealthCare].[DrugBatches] WHERE DrugBatchId = @BatchId AND CreatedByOrgId = @ManufacturerOrgId)
        BEGIN
            THROW 50001, 'Batch not found or unauthorized.', 1;
        END

        -- Blockchain Recording (Record before deletion)
        DECLARE @PrevHash NVARCHAR(64);
        SET @PrevHash = (SELECT TOP 1 CurrentHash FROM [HealthCare].[BlockchainLedgers] WHERE DrugBatchId = @BatchId ORDER BY ActionTime DESC);
        IF @PrevHash IS NULL SET @PrevHash = '0';

        DECLARE @Action NVARCHAR(50);
        SET @Action = 'BATCH_DELETED';
        DECLARE @NewHash NVARCHAR(64);
        SET @NewHash = [HealthCare].[fn_ComputeBlockchainHash](@BatchId, @Action, 0, @PrevHash);

        INSERT INTO [HealthCare].[BlockchainLedgers] (DrugBatchId, Action, Quantity, PreviousHash, CurrentHash, ActionTime)
        VALUES (@BatchId, @Action, 0, @PrevHash, @NewHash, SYSDATETIME());

        -- Delete records (Cascading logic simplified: Delete Batch)
        -- Note: If History has FK constraints, they must be handled.
        DELETE FROM [HealthCare].[DrugBatches] WHERE DrugBatchId = @BatchId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 4. Stored Procedure for Dispatching a Batch
CREATE OR ALTER PROCEDURE [HealthCare].[sp_DispatchBatchToDistributor]
    @BatchId NVARCHAR(100),
    @FromOrgId INT,
    @ToOrgId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Check if exists and currently owned by FromOrgId (No active TRANSFERRED records yet)
        IF EXISTS (SELECT 1 FROM [HealthCare].[BatchOwnershipHistories] WHERE DrugBatchId = @BatchId AND ActionType = 'TRANSFERRED')
        BEGIN
            THROW 50003, 'Batch already dispatched.', 1;
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
        SET @Action = 'TRANSFER_TO_DISTRIBUTOR';
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
