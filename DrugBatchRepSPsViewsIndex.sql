USE [DrugTracker];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*========================================================
  INDEXES (Created first where safe)
========================================================*/

/* DrugBatches */


CREATE NONCLUSTERED INDEX IX_DrugBatches_CreatedByOrgId_CreatedAt
ON [HealthCare].[DrugBatches] (CreatedByOrgId, CreatedAt DESC)
INCLUDE (DrugId, QuantityProduced);
GO

/* Drugs */
CREATE NONCLUSTERED INDEX IX_Drugs_DrugId_IsActive
ON [HealthCare].[Drugs] (DrugId, IsActive)
INCLUDE (DrugName);
GO

/* BatchOwnershipHistories */
CREATE NONCLUSTERED INDEX IX_BatchOwnershipHistories_ToOrgId_ActionTime
ON [HealthCare].[BatchOwnershipHistories] (ToOrgId, ActionTime DESC)
INCLUDE (DrugBatchId);
GO

CREATE NONCLUSTERED INDEX IX_BatchOwnershipHistories_Batch_ActionTime
ON [HealthCare].[BatchOwnershipHistories] (DrugBatchId, ActionTime DESC)
INCLUDE (ToOrgId);
GO

CREATE NONCLUSTERED INDEX IX_BatchOwnershipHistories_DrugBatchId_ActionTime
ON [HealthCare].[BatchOwnershipHistories] (DrugBatchId, ActionTime);
GO

/*========================================================
  VIEWS (READ OPERATIONS)
========================================================*/

/* Drug + Manufacturer + Batch details */
CREATE OR ALTER VIEW [HealthCare].[vw_DrugBatches_Details]
AS
SELECT
    b.DrugBatchId,
    b.DrugId,
    b.QuantityProduced,
    b.ManufactureDate,
    b.ExpiryDate,
    b.CreatedAt,

    d.DrugName,
    d.DrugCode,

    o.OrgId AS CreatedByOrgId,
    o.OrgName AS ManufacturerName
FROM [HealthCare].[DrugBatches] b
INNER JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
INNER JOIN [HealthCare].[Organizations] o ON b.CreatedByOrgId = o.OrgId;
GO

/* Active drug lookup */
CREATE OR ALTER VIEW [HealthCare].[vw_Drugs_Lookup]
AS
SELECT
    DrugId,
    DrugName
FROM [HealthCare].[Drugs]
WHERE IsActive = 1;
GO

/* Batches by manufacturer */
CREATE OR ALTER VIEW [HealthCare].[vw_DrugBatches_ByManufacturer]
AS
SELECT
    b.DrugBatchId,
    b.DrugId,
    d.DrugName,
    b.QuantityProduced,
	b.ManufactureDate,
    b.ExpiryDate,  
    b.CreatedAt,
    b.CreatedByOrgId
FROM [HealthCare].[DrugBatches] b
INNER JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId;
GO

/* Ownership history (human-readable) */
CREATE OR ALTER VIEW [HealthCare].[vw_BatchOwnershipHistory]
AS
SELECT
  h.OwnershipId,
    h.DrugBatchId,
    h.FromOrgId,        -- ✅ REQUIRED
    h.ToOrgId,          -- ✅ REQUIRED
    h.ActionType,
    h.PerformedBy,      -- ✅ REQUIRED
    h.ActionTime
FROM [HealthCare].[BatchOwnershipHistories] h
INNER JOIN [HealthCare].[Users] u ON h.PerformedBy = u.UserId
INNER JOIN [HealthCare].[Organizations] o ON h.ToOrgId = o.OrgId;
GO

/*========================================================
  STORED PROCEDURES (WRITE / COMPLEX READ)
========================================================*/

/* Add Drug Batch */
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

/* Add Dispatch / Ownership History */
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

/* Batch count by Drug */
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetBatchCountForDrug]
    @DrugId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS BatchCount
    FROM [HealthCare].[DrugBatches]
    WHERE DrugId = @DrugId;
END;
GO

/* Incoming dispatches (latest ownership only) */
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetIncomingDispatches]
    @ToOrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH LatestOwnership AS
    (
        SELECT
            DrugBatchId,
            ToOrgId,
            ROW_NUMBER() OVER (PARTITION BY DrugBatchId ORDER BY ActionTime DESC) AS rn
        FROM [HealthCare].[BatchOwnershipHistories]
    )
    SELECT
        b.DrugBatchId,
        d.DrugName,
        b.QuantityProduced,
        b.CreatedAt
    FROM LatestOwnership lo
    INNER JOIN [HealthCare].[DrugBatches] b ON lo.DrugBatchId = b.DrugBatchId
    INNER JOIN [HealthCare].[Drugs] d ON b.DrugId = d.DrugId
    WHERE lo.rn = 1
      AND lo.ToOrgId = @ToOrgId;
END;
GO

/* Update Drug Batch */
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

