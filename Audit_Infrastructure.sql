USE [DrugTracker];
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'HealthCare')
    EXEC ('CREATE SCHEMA HealthCare');
GO

/*========================================================
  1. Session Context SP
========================================================*/
CREATE OR ALTER PROCEDURE HealthCare.sp_SetUserSessionContext
    @UserId INT,
    @OrgId INT,
    @Role NVARCHAR(50),
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Optional: validate user & role (Basic check)
    -- IF NOT EXISTS (SELECT 1 FROM HealthCare.Users WHERE UserId = @UserId) ...

    EXEC sys.sp_set_session_context N'UserId', @UserId;
    EXEC sys.sp_set_session_context N'OrgId',  @OrgId;
    EXEC sys.sp_set_session_context N'Role',   @Role;
    EXEC sys.sp_set_session_context N'IPAddress', @IPAddress;
END
GO

/*========================================================
  2. Audit Tables
========================================================*/

-- Manufacturer Logs
IF OBJECT_ID('HealthCare.ManufacturerActivityLogs','U') IS NULL
CREATE TABLE HealthCare.ManufacturerActivityLogs (
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT,
    OrganizationId INT,
    Role NVARCHAR(50),
    ActionType NVARCHAR(50),
    TableName NVARCHAR(100),
    RecordId NVARCHAR(100),
    Description NVARCHAR(MAX),
    ActionTime DATETIME2(3) DEFAULT SYSDATETIME(),
    IPAddress NVARCHAR(50)
);
GO

-- Distributor Logs
IF OBJECT_ID('HealthCare.DistributorActivityLogs','U') IS NULL
CREATE TABLE HealthCare.DistributorActivityLogs (
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT,
    OrganizationId INT,
    Role NVARCHAR(50),
    ActionType NVARCHAR(50),
    TableName NVARCHAR(100),
    RecordId NVARCHAR(100),
    Description NVARCHAR(MAX),
    ActionTime DATETIME2(3) DEFAULT SYSDATETIME(),
    IPAddress NVARCHAR(50)
);
GO

-- Pharmacy Logs
IF OBJECT_ID('HealthCare.PharmacyActivityLogs','U') IS NULL
CREATE TABLE HealthCare.PharmacyActivityLogs (
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT,
    OrganizationId INT,
    Role NVARCHAR(50),
    ActionType NVARCHAR(50),
    TableName NVARCHAR(100),
    RecordId NVARCHAR(100),
    Description NVARCHAR(MAX),
    ActionTime DATETIME2(3) DEFAULT SYSDATETIME(),
    IPAddress NVARCHAR(50)
);
GO
