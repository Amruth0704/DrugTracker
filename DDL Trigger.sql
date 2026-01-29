USE DrugTracker;
GO

CREATE TRIGGER trg_PreventDrop_HealthCareTables
ON DATABASE
FOR DROP_TABLE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EventData XML = EVENTDATA();
    DECLARE @SchemaName SYSNAME;
    DECLARE @TableName SYSNAME;

    SET @SchemaName = @EventData.value('(/EVENT_INSTANCE/SchemaName)[1]', 'SYSNAME');
    SET @TableName  = @EventData.value('(/EVENT_INSTANCE/ObjectName)[1]', 'SYSNAME');

    -- List of protected tables
    IF @SchemaName = 'HealthCare'
       AND @TableName IN (
            'ActivityLogs',
            'BatchOwnershipHistories',
            'BlockchainLedgers',
            'DrugBatches',
            'Drugs',
            'Inventories',
            'Organizations',
            'Users'
       )
    BEGIN
        RAISERROR (
            'DROP TABLE is not allowed on protected HealthCare tables.',
            16, 1
        );
        ROLLBACK;
    END
END;
GO

DENY ALTER ON OBJECT::[HealthCare].[DrugBatches] TO PUBLIC;
DENY ALTER ON OBJECT::[HealthCare].[Drugs] TO PUBLIC;
DENY ALTER ON OBJECT::[HealthCare].[Inventories] TO PUBLIC;
DENY ALTER ON OBJECT::[HealthCare].[BatchOwnershipHistories] TO PUBLIC;
DENY ALTER ON OBJECT::[HealthCare].[Organizations] TO PUBLIC;
DENY ALTER ON OBJECT::[HealthCare].[Users] TO PUBLIC;
Go

