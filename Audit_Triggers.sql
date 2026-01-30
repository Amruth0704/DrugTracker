USE [DrugTracker];
GO

/*========================================================
  1. Manufacturer Trigger: DrugBatches
========================================================*/
CREATE OR ALTER TRIGGER HealthCare.TR_DrugBatches_Logging
ON HealthCare.DrugBatches
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId INT = CAST(SESSION_CONTEXT(N'UserId') AS INT);
    DECLARE @OrgId INT = CAST(SESSION_CONTEXT(N'OrgId') AS INT);
    DECLARE @Role NVARCHAR(50) = CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50));
    DECLARE @IP NVARCHAR(50) = CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50));

    -- INSERT
    INSERT INTO HealthCare.ManufacturerActivityLogs
    (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
    SELECT
        @UserId, @OrgId, @Role, 'INSERT', 'DrugBatches', i.DrugBatchId, 
        'New drug batch created. Qty: ' + CAST(i.QuantityProduced AS NVARCHAR(20)),
        SYSDATETIME(), @IP
    FROM inserted i
    LEFT JOIN deleted d ON i.DrugBatchId = d.DrugBatchId
    WHERE d.DrugBatchId IS NULL;

    -- UPDATE
    INSERT INTO HealthCare.ManufacturerActivityLogs
    (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
    SELECT
        @UserId, @OrgId, @Role, 'UPDATE', 'DrugBatches', i.DrugBatchId,
        'Batch updated. Old Qty: ' + CAST(d.QuantityProduced AS NVARCHAR(20)) + ' -> New Qty: ' + CAST(i.QuantityProduced AS NVARCHAR(20)),
        SYSDATETIME(), @IP
    FROM inserted i
    INNER JOIN deleted d ON i.DrugBatchId = d.DrugBatchId;

    -- DELETE
    INSERT INTO HealthCare.ManufacturerActivityLogs
    (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
    SELECT
        @UserId, @OrgId, @Role, 'DELETE', 'DrugBatches', d.DrugBatchId,
        'Batch deleted.',
        SYSDATETIME(), @IP
    FROM deleted d
    LEFT JOIN inserted i ON d.DrugBatchId = i.DrugBatchId
    WHERE i.DrugBatchId IS NULL;
END
GO

/*========================================================
  2. Distributor/Shared Trigger: BatchOwnershipHistories
========================================================*/
-- This table is touched by Manufacturer (Dispatch), Distributor (Receive/Dispatch), Pharmacy (Receive).
-- We need to decide which Log Table to insert into based on the Role or Context.
-- For simplicity, we can log to the table corresponding to the ACTOR's role.

CREATE OR ALTER TRIGGER HealthCare.TR_BatchOwnershipHistory_Logging
ON HealthCare.BatchOwnershipHistories
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId INT = CAST(SESSION_CONTEXT(N'UserId') AS INT);
    DECLARE @OrgId INT = CAST(SESSION_CONTEXT(N'OrgId') AS INT);
    DECLARE @Role NVARCHAR(50) = CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50));
    DECLARE @IP NVARCHAR(50) = CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50));

    -- Determine Target Table dynamically or just INSERT to all? No, that's redundant.
    -- Logic: If Role is Manufacturer -> ManufacturerActivityLogs
    --        If Role is Distributor  -> DistributorActivityLogs
    --        If Role is Pharmacy     -> PharmacyActivityLogs
    --        Else -> ManufacturerActivityLogs (Fallback)

    IF @Role = 'Manufacturer'
    BEGIN
        INSERT INTO HealthCare.ManufacturerActivityLogs
        (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
        SELECT @UserId, @OrgId, @Role, i.ActionType, 'BatchOwnershipHistories', i.DrugBatchId, 
               'Action: ' + i.ActionType + ' To Org: ' + CAST(i.ToOrgId AS NVARCHAR(20)), SYSDATETIME(), @IP
        FROM inserted i;
    END
    ELSE IF @Role = 'Distributor'
    BEGIN
        INSERT INTO HealthCare.DistributorActivityLogs
        (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
        SELECT @UserId, @OrgId, @Role, i.ActionType, 'BatchOwnershipHistories', i.DrugBatchId, 
               'Action: ' + i.ActionType + ' To Org: ' + CAST(i.ToOrgId AS NVARCHAR(20)), SYSDATETIME(), @IP
        FROM inserted i;
    END
    ELSE IF @Role = 'Pharmacy'
    BEGIN
        INSERT INTO HealthCare.PharmacyActivityLogs
        (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
        SELECT @UserId, @OrgId, @Role, i.ActionType, 'BatchOwnershipHistories', i.DrugBatchId, 
               'Action: ' + i.ActionType + ' To Org: ' + CAST(i.ToOrgId AS NVARCHAR(20)), SYSDATETIME(), @IP
        FROM inserted i;
    END
END
GO

/*========================================================
  3. Pharmacy Trigger: Inventories
========================================================*/
CREATE OR ALTER TRIGGER HealthCare.TR_Inventory_Logging
ON HealthCare.Inventories
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId INT = CAST(SESSION_CONTEXT(N'UserId') AS INT);
    DECLARE @OrgId INT = CAST(SESSION_CONTEXT(N'OrgId') AS INT);
    DECLARE @Role NVARCHAR(50) = CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50));
    DECLARE @IP NVARCHAR(50) = CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50));

    -- INSERT
    INSERT INTO HealthCare.PharmacyActivityLogs
    (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
    SELECT
        @UserId, @OrgId, @Role, 'INSERT', 'Inventories', i.DrugBatchId, 
        'Inventory Added. Avail: ' + CAST(i.AvailableQty AS NVARCHAR(20)),
        SYSDATETIME(), @IP
    FROM inserted i
    LEFT JOIN deleted d ON i.DrugBatchId = d.DrugBatchId
    WHERE d.DrugBatchId IS NULL;

    -- UPDATE
    INSERT INTO HealthCare.PharmacyActivityLogs
    (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
    SELECT
        @UserId, @OrgId, @Role, 'UPDATE', 'Inventories', i.DrugBatchId, 
        'Inventory Updated. Change: ' + CAST((i.AvailableQty - d.AvailableQty) AS NVARCHAR(20)),
        SYSDATETIME(), @IP
    FROM inserted i
    INNER JOIN deleted d ON i.DrugBatchId = d.DrugBatchId;

    -- DELETE
    INSERT INTO HealthCare.PharmacyActivityLogs
    (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, ActionTime, IPAddress)
    SELECT
        @UserId, @OrgId, @Role, 'DELETE', 'Inventories', d.DrugBatchId, 
        'Inventory Removed.',
        SYSDATETIME(), @IP
    FROM deleted d
    LEFT JOIN inserted i ON d.DrugBatchId = i.DrugBatchId
    WHERE i.DrugBatchId IS NULL;
END
GO
