using DrugTracker.Data;
using DrugTracker.Models;

using DrugTracker.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Implementations
{
    public class DrugBatchRepository : IDrugBatchRepository
    {
        private readonly DrugTrackerDbContext _context;

        public DrugBatchRepository(DrugTrackerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /*====================================================
          WRITE: Add Drug Batch
        ====================================================*/
        public async Task AddAsync(DrugBatch batch, int userId, int orgId, string role, string ipAddress)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_AddDrugBatch
                    @DrugBatchId,
                    @DrugId,
                    @QuantityProduced,
                    @ManufactureDate,
                    @ExpiryDate,
                    @CreatedByOrgId,
                    @UserId,
                    @OrgId,
                    @Role,
                    @IPAddress",
                new SqlParameter("@DrugBatchId", batch.DrugBatchId),
                new SqlParameter("@DrugId", batch.DrugId),
                new SqlParameter("@QuantityProduced", batch.QuantityProduced),
                new SqlParameter("@ManufactureDate", batch.ManufactureDate),
                new SqlParameter("@ExpiryDate", batch.ExpiryDate),
                new SqlParameter("@CreatedByOrgId", batch.CreatedByOrgId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@OrgId", orgId),
                new SqlParameter("@Role", role),
                new SqlParameter("@IPAddress", ipAddress)
            );
        }

        /*====================================================
          WRITE: Add Dispatch / Ownership History
        ====================================================*/
        public async Task AddDispatchRecordAsync(BatchOwnershipHistory history, int userId, int orgId, string role, string ipAddress)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_AddBatchOwnershipHistory
                    @DrugBatchId,
                    @FromOrgId,
                    @ToOrgId,
                    @ActionType,
                    @PerformedBy,
                    @UserId,
                    @OrgId,
                    @Role,
                    @IPAddress",
                new SqlParameter("@DrugBatchId", history.DrugBatchId),
                new SqlParameter("@FromOrgId", (object?)history.FromOrgId ?? DBNull.Value),
                new SqlParameter("@ToOrgId", history.ToOrgId),
                new SqlParameter("@ActionType", history.ActionType),
                new SqlParameter("@PerformedBy", history.PerformedBy),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@OrgId", orgId),
                new SqlParameter("@Role", role),
                new SqlParameter("@IPAddress", ipAddress)
            );
        }

        /*====================================================
          READ: Batch By Id
        ====================================================*/
        public async Task<DrugBatch?> GetByBatchIdAsync(string batchId)
        {
            return await _context.DrugBatches
                .Include(b => b.Drug)
                .Include(b => b.CreatedByOrg)
                .Where(b => b.DrugBatchId == batchId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        /*====================================================
          READ: Batch Count By Drug
        ====================================================*/
        public async Task<int> GetBatchCountForDrugAsync(int drugId)
        {
            var result = await _context.Set<BatchCountResult>()
                .FromSqlRaw(
                    @"EXEC HealthCare.sp_GetBatchCountForDrug @DrugId",
                    new SqlParameter("@DrugId", drugId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return result?.BatchCount ?? 0;
        }

        /*====================================================
          READ: Drug Name Lookup
        ====================================================*/
        public async Task<string?> GetDrugNameByIdAsync(int drugId)
        {
            return await _context.Drugs
                .FromSqlRaw(
                    @"SELECT DrugId, DrugName
                      FROM HealthCare.vw_Drugs_Lookup
                      WHERE DrugId = @DrugId",
                    new SqlParameter("@DrugId", drugId))
                .AsNoTracking()
                .Select(d => d.DrugName)
                .FirstOrDefaultAsync();
        }

        /*====================================================
          READ: Batches By Manufacturer
        ====================================================*/
        public async Task<IEnumerable<DrugBatch>> GetBatchesByManufacturerAsync(int orgId)
        {
            return await _context.DrugBatches
                .Include(b => b.Drug)
                .Where(b => b.CreatedByOrgId == orgId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }





        /*====================================================
          READ: Incoming Dispatches
        ====================================================*/
        public async Task<IEnumerable<DrugBatch>> GetIncomingDispatchesAsync(int toOrgId)
        {
            return await _context.DrugBatches
                .FromSqlRaw(
                    @"SELECT *
                      FROM HealthCare.vw_IncomingDispatches
                      WHERE ToOrgId = @ToOrgId",
                    new SqlParameter("@ToOrgId", toOrgId))
                .Include(b => b.Drug)
                .AsNoTracking()
                .ToListAsync();
        }

        /*====================================================
          READ: Ownership History
        ====================================================*/
        public async Task<IEnumerable<BatchOwnershipHistory>> GetOwnershipHistoryAsync(string batchId)
        {
            return await _context.BatchOwnershipHistories
                .FromSqlRaw(
                    @"SELECT *
                      FROM HealthCare.vw_BatchOwnershipHistory
                      WHERE DrugBatchId = @DrugBatchId
                      ORDER BY ActionTime",
                    new SqlParameter("@DrugBatchId", batchId))
                .AsNoTracking()
                .ToListAsync();
        }

        /*====================================================
          WRITE: Update Batch
        ====================================================*/
        public async Task UpdateAsync(DrugBatch batch, int userId, int orgId, string role, string ipAddress)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_UpdateDrugBatch
                    @DrugBatchId,
                    @QuantityProduced,
                    @ManufactureDate,
                    @ExpiryDate,
                    @UserId,
                    @OrgId,
                    @Role,
                    @IPAddress",
                new SqlParameter("@DrugBatchId", batch.DrugBatchId),
                new SqlParameter("@QuantityProduced", batch.QuantityProduced),
                new SqlParameter("@ManufactureDate", batch.ManufactureDate),
                new SqlParameter("@ExpiryDate", batch.ExpiryDate),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@OrgId", orgId),
                new SqlParameter("@Role", role),
                new SqlParameter("@IPAddress", ipAddress)
            );
        }
            /*====================================================
          READ: Batches for Distributor (History + Current)
        ====================================================*/
        public async Task<IEnumerable<DrugBatch>> GetBatchesForDistributorAsync(int distId)
        {
             // Get BatchIds where Distributor was involved (either as Receiver or Sender)
             var batchIds = await _context.BatchOwnershipHistories
                 .Where(h => h.ToOrgId == distId || h.FromOrgId == distId)
                 .Select(h => h.DrugBatchId)
                 .Distinct()
                 .ToListAsync();
            
             // Fetch Batches
             return await _context.DrugBatches
                 .Include(b => b.Drug)
                 .Where(b => batchIds.Contains(b.DrugBatchId))
                 .OrderByDescending(b => b.CreatedAt)
                 .AsNoTracking()
                 .ToListAsync();
        }

        /*====================================================
          WRITE: Delete Drug Batch
        ====================================================*/
        public async Task DeleteAsync(string batchId, int userId, int orgId, string role, string ipAddress)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_DeleteDrugBatch
                    @DrugBatchId,
                    @UserId,
                    @OrgId,
                    @Role,
                    @IPAddress",
                new SqlParameter("@DrugBatchId", batchId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@OrgId", orgId),
                new SqlParameter("@Role", role),
                new SqlParameter("@IPAddress", ipAddress)
            );
        }
    }

    /*====================================================
      Helper DTO for COUNT SP
    ====================================================*/
    public class BatchCountResult
    {
        public int BatchCount { get; set; }
    }
}
