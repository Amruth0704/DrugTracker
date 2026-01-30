CREATE OR ALTER PROCEDURE HealthCare.sp_DeleteDrugBatch
    @DrugBatchId NVARCHAR(100),
    @UserId INT,
    @OrgId INT,
    @Role NVARCHAR(50),
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Set Session Context for Audit Trigger
    EXEC HealthCare.sp_SetUserSessionContext @UserId, @OrgId, @Role, @IPAddress;

    DELETE FROM HealthCare.DrugBatches
    WHERE DrugBatchId = @DrugBatchId;
END
GO
