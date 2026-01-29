USE [DrugTracker];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*========================================================
  1. Get Organization By Id
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetOrganizationById]
    @OrgId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        OrgId,
        OrgName,
        OrgType,
        IsActive,
        CreatedAt
    FROM [HealthCare].[Organizations]
    WHERE OrgId = @OrgId;
END;
GO

/*========================================================
  2. Get User By Username (Includes Organization)
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_GetUserByUsername]
    @UserName NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.UserId,
        u.UserName,
        u.PasswordHash,
        u.Role,
        u.OrgId,
        u.IsActive,
        u.CreatedAt,
        u.AccessFailedCount,
        u.LockoutEnd,

        o.OrgId AS Organization_OrgId,
        o.OrgName,
        o.OrgType,
        o.IsActive AS Organization_IsActive,
        o.CreatedAt AS Organization_CreatedAt
    FROM [HealthCare].[Users] u
    INNER JOIN [HealthCare].[Organizations] o
        ON u.OrgId = o.OrgId
    WHERE u.UserName = @UserName;
END;
GO

/*========================================================
  3. Validate User (Username + PasswordHash)
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_ValidateUser]
    @UserName NVARCHAR(150),
    @PasswordHash VARBINARY(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.UserId,
        u.UserName,
        u.PasswordHash,
        u.Role,
        u.OrgId,
        u.IsActive,
        u.CreatedAt,
        u.AccessFailedCount,
        u.LockoutEnd,

        o.OrgId AS Organization_OrgId,
        o.OrgName,
        o.OrgType,
        o.IsActive AS Organization_IsActive,
        o.CreatedAt AS Organization_CreatedAt
    FROM [HealthCare].[Users] u
    INNER JOIN [HealthCare].[Organizations] o
        ON u.OrgId = o.OrgId
    WHERE 
        u.UserName = @UserName
        AND u.PasswordHash = @PasswordHash
        AND u.IsActive = 1;
END;
GO

/*========================================================
  4. Update User
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_UpdateUser]
    @UserId INT,
    @Role NVARCHAR(30),
    @OrgId INT,
    @IsActive BIT,
    @AccessFailedCount INT,
    @LockoutEnd DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [HealthCare].[Users]
    SET
        Role = @Role,
        OrgId = @OrgId,
        IsActive = @IsActive,
        AccessFailedCount = @AccessFailedCount,
        LockoutEnd = @LockoutEnd
    WHERE UserId = @UserId;
END;
GO

/*========================================================
  5. Log User Activity (Audit)
========================================================*/
CREATE OR ALTER PROCEDURE [HealthCare].[sp_LogUserActivity]
    @UserId INT,
    @Action NVARCHAR(100),
    @EntityType NVARCHAR(50),
    @EntityId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [HealthCare].[ActivityLogs]
    (
        UserId,
        Action,
        EntityType,
        EntityId
    )
    VALUES
    (
        @UserId,
        @Action,
        @EntityType,
        @EntityId
    );
END;
GO
