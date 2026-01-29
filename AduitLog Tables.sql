USE DrugTracker;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'HealthCare')
    EXEC ('CREATE SCHEMA HealthCare');
GO

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
