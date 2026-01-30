USE DrugTracker;
GO

-- Drugs
DENY INSERT, UPDATE, DELETE ON HealthCare.Drugs TO PUBLIC;
GO

-- Organizations
DENY INSERT, UPDATE, DELETE ON HealthCare.Organizations TO PUBLIC;
GO

-- Users
DENY INSERT, UPDATE, DELETE ON HealthCare.Users TO PUBLIC;
GO


REVOKE  UPDATE, DELETE
ON HealthCare.DistributorActivityLogs
FROM PUBLIC;

REVOKE UPDATE, DELETE
ON HealthCare.ManufacturerActivityLogs
FROM PUBLIC;

REVOKE  UPDATE, DELETE
ON HealthCare.PharmacyActivityLogs
FROM PUBLIC;
GO



DENY ALTER, CONTROL
ON HealthCare.DistributorActivityLogs
TO PUBLIC;

DENY ALTER, CONTROL
ON HealthCare.ManufacturerActivityLogs
TO PUBLIC;

DENY ALTER, CONTROL
ON HealthCare.PharmacyActivityLogs
TO PUBLIC;
GO



CREATE TRIGGER trg_BlockActivityLogsDDL
ON DATABASE
FOR DROP_TABLE, ALTER_TABLE
AS
BEGIN
    DECLARE @ObjectName SYSNAME;
    SET @ObjectName = EVENTDATA().value(
        '(/EVENT_INSTANCE/ObjectName)[1]',
        'SYSNAME'
    );

    IF @ObjectName IN (
        'DistributorActivityLogs',
        'ManufacturerActivityLogs',
        'PharmacyActivityLogs'
    )
    BEGIN
        ROLLBACK;
        RAISERROR (
            'Action not allowed on Activity Log tables.',
            16,
            1
        );
    END
END;
GO



USE DrugTracker;
GO

DENY DELETE ON HealthCare.Inventories TO PUBLIC;
GO

Delete HealthCare.Inventories


CREATE OR ALTER TRIGGER HealthCare.TR_Block_Inventory_Delete
ON HealthCare.Inventories
INSTEAD OF DELETE
AS
BEGIN
    THROW 50020, 'DELETE is permanently disabled on Inventories table. Use UPDATE instead.', 1;
END
GO
