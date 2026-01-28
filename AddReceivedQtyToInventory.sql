USE [DrugTracker];
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[HealthCare].[Inventories]') 
    AND name = 'ReceivedQty'
)
BEGIN
    ALTER TABLE [HealthCare].[Inventories]
    ADD [ReceivedQty] INT NOT NULL DEFAULT 0;
    
    -- Initialize ReceivedQty with the current AvailableQty value
    -- as we assume they were identical upon initial receipt.
    EXEC('UPDATE [HealthCare].[Inventories] SET [ReceivedQty] = [AvailableQty]');
END
GO
