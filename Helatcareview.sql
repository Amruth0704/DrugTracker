USE [DrugTracker];
GO

CREATE OR ALTER VIEW [HealthCare].[vw_UsersWithOrganization]
AS
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

    o.OrgName,
    o.OrgType,
    o.IsActive AS OrganizationIsActive,
    o.CreatedAt AS OrganizationCreatedAt
FROM [HealthCare].[Users] u
INNER JOIN [HealthCare].[Organizations] o
    ON u.OrgId = o.OrgId;
GO

USE [DrugTracker];
GO

CREATE OR ALTER VIEW [HealthCare].[vw_IncomingDispatches]
AS
WITH LatestOwnership AS
(
    SELECT
        h.DrugBatchId,
        h.ToOrgId,
        h.ActionTime,
        ROW_NUMBER() OVER (
            PARTITION BY h.DrugBatchId
            ORDER BY h.ActionTime DESC
        ) AS rn
    FROM [HealthCare].[BatchOwnershipHistories] h
)
SELECT
    b.DrugBatchId,
    b.DrugId,
    b.QuantityProduced,
    b.ManufactureDate,
    b.ExpiryDate,
    b.CreatedByOrgId,
    b.CreatedAt,
    lo.ToOrgId
FROM LatestOwnership lo
INNER JOIN [HealthCare].[DrugBatches] b
    ON lo.DrugBatchId = b.DrugBatchId
WHERE lo.rn = 1;
GO
