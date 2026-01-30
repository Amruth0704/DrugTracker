USE [DrugTracker];
GO

-- Stored Procedure for Creating a New Drug Batch
CREATE OR ALTER PROCEDURE [HealthCare].[sp_CreateDrugBatch]
    @DrugId INT,
    @QuantityProduced INT,
    @ManufactureDate DATETIME,
    @ExpiryDate DATETIME,
    @CreatedByOrgId INT,
    @UserId INT,
    @GeneratedBatchId NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- 1. Generate Batch ID (Prefix from Drug Name + Suffix)
        DECLARE @DrugName NVARCHAR(100);
        SET @DrugName = (SELECT DrugName FROM [HealthCare].[Drugs] WHERE DrugId = @DrugId);
        IF @DrugName IS NULL THROW 50007, 'Drug not found.', 1;

        DECLARE @Prefix NVARCHAR(3);
        SET @Prefix = UPPER(LEFT(@DrugName, 3));
        DECLARE @Count INT;
        SET @Count = (SELECT COUNT(*) FROM [HealthCare].[DrugBatches] WHERE DrugId = @DrugId);
        
        -- Formatted as PRE001, PRE002, etc. (3-character prefix + 3-digit suffix)
        SET @GeneratedBatchId = @Prefix + FORMAT(@Count + 1, 'D3');

        -- 2. Insert into DrugBatches
        INSERT INTO [HealthCare].[DrugBatches] (DrugBatchId, DrugId, QuantityProduced, ManufactureDate, ExpiryDate, CreatedByOrgId, CreatedAt)
        VALUES (@GeneratedBatchId, @DrugId, @QuantityProduced, @ManufactureDate, @ExpiryDate, @CreatedByOrgId, SYSDATETIME());

        -- 3. Initial Ownership History (CREATED)
        INSERT INTO [HealthCare].[BatchOwnershipHistories] (DrugBatchId, ActionType, ToOrgId, PerformedBy, ActionTime)
        VALUES (@GeneratedBatchId, 'CREATED', @CreatedByOrgId, @UserId, SYSDATETIME());

        -- 4. Blockchain Recording (BATCH_CREATED)
        DECLARE @Action NVARCHAR(50);
        SET @Action = 'BATCH_CREATED';
        DECLARE @PrevHash NVARCHAR(64);
        SET @PrevHash = '0'; -- Genesis hash for a new batch
        
        -- Use the already created helper function
        DECLARE @NewHash NVARCHAR(64);
        SET @NewHash = [HealthCare].[fn_ComputeBlockchainHash](@GeneratedBatchId, @Action, @QuantityProduced, @PrevHash);

        INSERT INTO [HealthCare].[BlockchainLedgers] (DrugBatchId, Action, ToOrgId, Quantity, PreviousHash, CurrentHash, ActionTime)
        VALUES (@GeneratedBatchId, @Action, @CreatedByOrgId, @QuantityProduced, @PrevHash, @NewHash, SYSDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
