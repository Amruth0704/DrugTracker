USE [DrugTracker];
GO

/*========================================================
  Update sp_AddDrugBatch
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_AddDrugBatch]
    @DrugBatchId NVARCHAR(100),
    @DrugId INT,
    @QuantityProduced INT,
    @ManufactureDate DATETIME2(7),
    @ExpiryDate DATETIME2(7),
    @CreatedByOrgId INT,
    
    -- Audit Params
    @UserId INT,
    @OrgId INT,          -- The Org performing the action (should match CreatedByOrgId usually)
    @Role NVARCHAR(50),
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Set Session Context
    EXEC HealthCare.sp_SetUserSessionContext @UserId, @OrgId, @Role, @IPAddress;

    -- 2. Perform Action (Trigger will catch it)
    INSERT INTO [HealthCare].[DrugBatches]
    (
        DrugBatchId,
        DrugId,
        QuantityProduced,
        ManufactureDate,
        ExpiryDate,
        CreatedByOrgId
    )
    VALUES
    (
        @DrugBatchId,
        @DrugId,
        @QuantityProduced,
        @ManufactureDate,
        @ExpiryDate,
        @CreatedByOrgId
    );
END;
GO

/*========================================================
  Update sp_UpdateDrugBatch
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_UpdateDrugBatch]
    @DrugBatchId NVARCHAR(100),
    @QuantityProduced INT,
    @ManufactureDate DATETIME2(7),
    @ExpiryDate DATETIME2(7),

    -- Audit Params
    @UserId INT,
    @OrgId INT,
    @Role NVARCHAR(50),
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    EXEC HealthCare.sp_SetUserSessionContext @UserId, @OrgId, @Role, @IPAddress;

    UPDATE [HealthCare].[DrugBatches]
    SET
        QuantityProduced = @QuantityProduced,
        ManufactureDate = @ManufactureDate,
        ExpiryDate = @ExpiryDate
    WHERE DrugBatchId = @DrugBatchId;
END;
GO

/*========================================================
  Update sp_AddBatchOwnershipHistory
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_AddBatchOwnershipHistory]
    @DrugBatchId NVARCHAR(100),
    @FromOrgId INT,
    @ToOrgId INT,
    @ActionType NVARCHAR(30),
    @PerformedBy INT, -- This is the user ID, redundant if in AuditParams, but kept for table schema

    -- Audit Params
    @UserId INT,
    @OrgId INT,
    @Role NVARCHAR(50),
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    EXEC HealthCare.sp_SetUserSessionContext @UserId, @OrgId, @Role, @IPAddress;

    INSERT INTO [HealthCare].[BatchOwnershipHistories]
    (
        DrugBatchId,
        FromOrgId,
        ToOrgId,
        ActionType,
        PerformedBy
    )
    VALUES
    (
        @DrugBatchId,
        @FromOrgId,
        @ToOrgId,
        @ActionType,
        @PerformedBy
    );
END;
GO

/*========================================================
  Update sp_AddOrUpdateInventory
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_AddOrUpdateInventory]
    @DrugBatchId NVARCHAR(100),
    @PharmacyOrgId INT,
    @AvailableQty INT,
    @ReceivedQty INT,

    -- Audit Params
    @UserId INT,
    @OrgId INT, -- Pharmacy Org
    @Role NVARCHAR(50), 
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    EXEC HealthCare.sp_SetUserSessionContext @UserId, @OrgId, @Role, @IPAddress;

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
