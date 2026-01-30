USE [DrugTracker];
GO

-- 1. Create 3 Separate Activity Log Tables
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ManufacturerActivityLogs' AND schema_id = SCHEMA_ID('HealthCare'))
BEGIN
    CREATE TABLE [HealthCare].[ManufacturerActivityLogs] (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        OrganizationId INT NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        TableName NVARCHAR(100) NOT NULL,
        RecordId NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX),
        ActionTime DATETIME DEFAULT SYSDATETIME(),
        IPAddress NVARCHAR(50) NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DistributorActivityLogs' AND schema_id = SCHEMA_ID('HealthCare'))
BEGIN
    CREATE TABLE [HealthCare].[DistributorActivityLogs] (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        OrganizationId INT NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        TableName NVARCHAR(100) NOT NULL,
        RecordId NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX),
        ActionTime DATETIME DEFAULT SYSDATETIME(),
        IPAddress NVARCHAR(50) NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PharmacyActivityLogs' AND schema_id = SCHEMA_ID('HealthCare'))
BEGIN
    CREATE TABLE [HealthCare].[PharmacyActivityLogs] (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        OrganizationId INT NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        TableName NVARCHAR(100) NOT NULL,
        RecordId NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX),
        ActionTime DATETIME DEFAULT SYSDATETIME(),
        IPAddress NVARCHAR(50) NULL
    );
END
GO

-- 2. Create a Central Logging Stored Procedure (Internal use by Triggers)
CREATE OR ALTER PROCEDURE [HealthCare].[sp_InsertRoleBasedActivityLog]
    @ActionType NVARCHAR(50),
    @TableName NVARCHAR(100),
    @RecordId NVARCHAR(100),
    @Description NVARCHAR(MAX)
AS
BEGIN
    DECLARE @UserId INT;
    SET @UserId = CAST(SESSION_CONTEXT(N'UserId') AS INT);
    DECLARE @OrgId INT;
    SET @OrgId = CAST(SESSION_CONTEXT(N'OrgId') AS INT);
    DECLARE @Role NVARCHAR(50);
    SET @Role = CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50));
    DECLARE @IP NVARCHAR(50);
    SET @IP = CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50));

    -- If no context is set, we don't log (or you could log to a system table)
    IF @Role IS NULL RETURN;

    IF @Role = 'Manufacturer'
    BEGIN
        INSERT INTO [HealthCare].[ManufacturerActivityLogs] (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, IPAddress)
        VALUES (@UserId, @OrgId, @Role, @ActionType, @TableName, @RecordId, @Description, @IP);
    END
    ELSE IF @Role = 'Distributor'
    BEGIN
        INSERT INTO [HealthCare].[DistributorActivityLogs] (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, IPAddress)
        VALUES (@UserId, @OrgId, @Role, @ActionType, @TableName, @RecordId, @Description, @IP);
    END
    ELSE IF @Role = 'Pharmacy'
    BEGIN
        INSERT INTO [HealthCare].[PharmacyActivityLogs] (UserId, OrganizationId, Role, ActionType, TableName, RecordId, Description, IPAddress)
        VALUES (@UserId, @OrgId, @Role, @ActionType, @TableName, @RecordId, @Description, @IP);
    END
END
GO

-- 3. Triggers for Automatic Logging

-- Trigger for DrugBatches (Manufacturer/Distributor updates)
CREATE OR ALTER TRIGGER [HealthCare].[TR_DrugBatches_Logging]
ON [HealthCare].[DrugBatches]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Action NVARCHAR(50);
    DECLARE @RecordId NVARCHAR(100);
    DECLARE @Desc NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted) -- UPDATE
    BEGIN
        SET @Action = 'UPDATE';
        SELECT @RecordId = DrugBatchId FROM inserted;
        SET @Desc = 'Batch details updated.';
    END
    ELSE IF EXISTS (SELECT * FROM inserted) -- INSERT
    BEGIN
        SET @Action = 'INSERT';
        SELECT @RecordId = DrugBatchId FROM inserted;
        SET @Desc = 'New drug batch created.';
    END
    ELSE IF EXISTS (SELECT * FROM deleted) -- DELETE
    BEGIN
        SET @Action = 'DELETE';
        SELECT @RecordId = DrugBatchId FROM deleted;
        SET @Desc = 'Drug batch deleted.';
    END

    IF @RecordId IS NOT NULL
        EXEC [HealthCare].[sp_InsertRoleBasedActivityLog] @Action, 'DrugBatches', @RecordId, @Desc;
END
GO

-- Trigger for BatchOwnershipHistories (Capture Transfers/Receives/Sells)
CREATE OR ALTER TRIGGER [HealthCare].[TR_BatchOwnership_Logging]
ON [HealthCare].[BatchOwnershipHistories]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RecordId NVARCHAR(100);
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Desc NVARCHAR(MAX);

    SELECT @RecordId = DrugBatchId, @ActionType = ActionType FROM inserted;
    SET @Desc = 'Batch ownership action: ' + ISNULL(@ActionType, 'UNKNOWN');

    IF @RecordId IS NOT NULL
        EXEC [HealthCare].[sp_InsertRoleBasedActivityLog] @ActionType, 'BatchOwnershipHistories', @RecordId, @Desc;
END
GO

-- Trigger for Inventories (Pharmacy specific stock changes)
CREATE OR ALTER TRIGGER [HealthCare].[TR_Inventories_Logging]
ON [HealthCare].[Inventories]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Action NVARCHAR(50);
    DECLARE @RecordId NVARCHAR(100);
    DECLARE @Desc NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @Action = 'UPDATE_STOCK';
        SELECT @RecordId = DrugBatchId FROM inserted;
        SET @Desc = 'Inventory stock updated.';
    END
    ELSE
    BEGIN
        SET @Action = 'INITIAL_STOCK';
        SELECT @RecordId = DrugBatchId FROM inserted;
        SET @Desc = 'Batch added to inventory.';
    END

    IF @RecordId IS NOT NULL
        EXEC [HealthCare].[sp_InsertRoleBasedActivityLog] @Action, 'Inventories', @RecordId, @Desc;
END
GO
