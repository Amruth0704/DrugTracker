ALTER TABLE [HealthCare].[DrugBatches]
ADD CONSTRAINT CK_DrugBatches_Expiry_Min3Years
CHECK (
    ExpiryDate = DATEADD(YEAR, 3,ManufactureDate)
);
GO

