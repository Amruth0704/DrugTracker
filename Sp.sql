CREATE OR ALTER PROCEDURE HealthCare.sp_SetUserSessionContext
    @UserId INT,
    @OrgId INT,
    @Role NVARCHAR(50),
    @IPAddress NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Optional: validate user & role
    IF NOT EXISTS (
        SELECT 1
        FROM HealthCare.Users
        WHERE UserId = @UserId
          AND OrgId = @OrgId
          AND Role = @Role
          AND IsActive = 1
    )
    BEGIN
        THROW 50001, 'Invalid user context', 1;
    END

    EXEC sys.sp_set_session_context N'UserId', @UserId;
    EXEC sys.sp_set_session_context N'OrgId',  @OrgId;
    EXEC sys.sp_set_session_context N'Role',   @Role;
    EXEC sys.sp_set_session_context N'IPAddress', @IPAddress;
END
GO
