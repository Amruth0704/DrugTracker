CREATE OR ALTER TRIGGER HealthCare.TR_DrugBatches_Logging
ON HealthCare.DrugBatches
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- INSERT
    INSERT INTO HealthCare.ManufacturerActivityLogs
    SELECT
        CAST(SESSION_CONTEXT(N'UserId') AS INT),
        CAST(SESSION_CONTEXT(N'OrgId') AS INT),
        CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50)),
        'INSERT',
        'DrugBatches',
        i.DrugBatchId,
        'New drug batch created.',
        SYSDATETIME(),
        CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50))
    FROM inserted i
    LEFT JOIN deleted d ON i.DrugBatchId = d.DrugBatchId
    WHERE d.DrugBatchId IS NULL;

    -- UPDATE
    INSERT INTO HealthCare.ManufacturerActivityLogs
    SELECT
        CAST(SESSION_CONTEXT(N'UserId') AS INT),
        CAST(SESSION_CONTEXT(N'OrgId') AS INT),
        CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50)),
        'UPDATE',
        'DrugBatches',
        i.DrugBatchId,
        'Batch details updated.',
        SYSDATETIME(),
        CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50))
    FROM inserted i
    INNER JOIN deleted d ON i.DrugBatchId = d.DrugBatchId;

    -- DELETE
    INSERT INTO HealthCare.ManufacturerActivityLogs
    SELECT
        CAST(SESSION_CONTEXT(N'UserId') AS INT),
        CAST(SESSION_CONTEXT(N'OrgId') AS INT),
        CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(50)),
        'DELETE',
        'DrugBatches',
        d.DrugBatchId,
        'Drug batch deleted.',
        SYSDATETIME(),
        CAST(SESSION_CONTEXT(N'IPAddress') AS NVARCHAR(50))
    FROM deleted d
    LEFT JOIN inserted i ON d.DrugBatchId = i.DrugBatchId
    WHERE i.DrugBatchId IS NULL;
END
GO
