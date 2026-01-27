-- ======================================================
-- Convert BlockchainLedger to SQL Server Append-Only Ledger
-- ======================================================

-- 1. Drop existing table if it exists
IF OBJECT_ID('[HealthCare].[BlockchainLedgers]', 'U') IS NOT NULL
BEGIN
    DROP TABLE [HealthCare].[BlockchainLedgers];
END
GO

-- 2. Create as Append-Only Ledger Table
-- This requires SQL Server 2022 or Azure SQL Database
CREATE TABLE [HealthCare].[BlockchainLedgers]
(
    [LedgerId] INT IDENTITY(1,1) PRIMARY KEY,
    [DrugBatchId] NVARCHAR(100) NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [FromOrgId] INT NULL,
    [ToOrgId] INT NULL,
    [Quantity] INT NULL,
    [ActionTime] DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
    [PreviousHash] NVARCHAR(MAX) NULL,
    [CurrentHash] NVARCHAR(MAX) NULL
)
WITH (LEDGER = ON (APPEND_ONLY = ON));
GO

-- 3. Verify the Ledger configuration
SELECT name, ledger_type_desc, is_append_only 
FROM sys.tables 
WHERE name = 'BlockchainLedgers' AND schema_id = SCHEMA_ID('HealthCare');
GO
