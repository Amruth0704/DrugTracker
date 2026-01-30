USE [DrugTracker];
GO

-- 1. Check and add PreviousHash if missing
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('[HealthCare].[BlockchainLedgers]') 
               AND name = 'PreviousHash')
BEGIN
    ALTER TABLE [HealthCare].[BlockchainLedgers] ADD [PreviousHash] NVARCHAR(MAX) NULL;
    PRINT 'Added PreviousHash column to BlockchainLedgers table.';
END
ELSE
BEGIN
    PRINT 'PreviousHash column already exists.';
END

-- 2. Check and add CurrentHash if missing
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('[HealthCare].[BlockchainLedgers]') 
               AND name = 'CurrentHash')
BEGIN
    ALTER TABLE [HealthCare].[BlockchainLedgers] ADD [CurrentHash] NVARCHAR(MAX) NULL;
    PRINT 'Added CurrentHash column to BlockchainLedgers table.';
END
ELSE
BEGIN
    PRINT 'CurrentHash column already exists.';
END
GO

-- 3. Verify final schema
SELECT Table_Name, Column_Name, Data_Type 
FROM Information_Schema.Columns 
WHERE Table_Name = 'BlockchainLedgers' AND Table_Schema = 'HealthCare';
GO
