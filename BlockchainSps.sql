-- already assumed created, shown for clarity
CREATE OR ALTER VIEW [HealthCare].[vw_BlockchainLedger_ByBatch]
AS
SELECT
    LedgerId,
    DrugBatchId,
    Action,
    FromOrgId,
    ToOrgId,
    Quantity,
    ActionTime
FROM [HealthCare].[BlockchainLedgers];
GO


-- already assumed created
CREATE OR ALTER PROCEDURE [HealthCare].[sp_AddBlockchainLedgerEntry]
    @DrugBatchId NVARCHAR(100),
    @Action NVARCHAR(50),
    @FromOrgId INT = NULL,
    @ToOrgId INT = NULL,
    @Quantity INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [HealthCare].[BlockchainLedgers]
    (
        DrugBatchId,
        Action,
        FromOrgId,
        ToOrgId,
        Quantity
    )
    VALUES
    (
        @DrugBatchId,
        @Action,
        @FromOrgId,
        @ToOrgId,
        @Quantity
    );
END;
GO
