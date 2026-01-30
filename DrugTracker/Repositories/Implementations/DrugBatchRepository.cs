using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Implementations
{
    public class DrugBatchRepository : IDrugBatchRepository
    {
        private readonly DrugTrackerDbContext _context;

        public DrugBatchRepository(DrugTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<DrugBatch?> GetByBatchIdAsync(string batchId)
        {
            return await _context.DrugBatches
                .Include(b => b.Drug)
                .Include(b => b.CreatedByOrg)
                .FirstOrDefaultAsync(b => b.DrugBatchId == batchId);
        }

        public async Task<IEnumerable<BatchOwnershipHistory>> GetOwnershipHistoryAsync(string batchId)
        {
            return await _context.BatchOwnershipHistories
                .Include(h => h.User)
                .Include(h => h.ToOrg)
                .Where(h => h.DrugBatchId == batchId)
                .OrderBy(h => h.ActionTime)
                .ToListAsync();
        }

        public async Task<string> CreateBatchSPAsync(int drugId, int quantity, DateTime mfgDate, DateTime expDate, int orgId, int userId)
        {
            var batchIdParam = new SqlParameter
            {
                ParameterName = "@GeneratedBatchId",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 100,
                Direction = System.Data.ParameterDirection.Output
            };

            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_CreateDrugBatch] @DrugId = {0}, @QuantityProduced = {1}, @ManufactureDate = {2}, @ExpiryDate = {3}, @CreatedByOrgId = {4}, @UserId = {5}, @GeneratedBatchId = @GeneratedBatchId OUTPUT",
                drugId, quantity, mfgDate, expDate, orgId, userId, batchIdParam);

            return batchIdParam.Value?.ToString() ?? string.Empty;
        }

        public async Task<IEnumerable<DrugTracker.Models.DTOs.ManufacturerDashboardRow>> GetManufacturerDashboardDataAsync(int orgId)
        {
            return await _context.ManufacturerDashboardData
                .FromSqlRaw("EXEC [HealthCare].[sp_GetManufacturerDashboardData] @ManufacturerOrgId = {0}", orgId)
                .ToListAsync();
        }

        public async Task UpdateManufacturerBatchAsync(string batchId, int drugId, int quantity, DateTime mfgDate, DateTime expDate, int orgId)
        {
            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_UpdateBatchByManufacturer] @BatchId = {0}, @NewDrugId = {1}, @NewQuantity = {2}, @NewManufactureDate = {3}, @NewExpiryDate = {4}, @ManufacturerOrgId = {5}",
                batchId, drugId, quantity, mfgDate, expDate, orgId);
        }

        public async Task DeleteManufacturerBatchAsync(string batchId, int orgId)
        {
            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_DeleteBatchByManufacturer] @BatchId = {0}, @ManufacturerOrgId = {1}",
                batchId, orgId);
        }

        public async Task DispatchToDistributorSPAsync(string batchId, int fromOrgId, int toOrgId, int userId)
        {
            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_DispatchBatchToDistributor] @BatchId = {0}, @FromOrgId = {1}, @ToOrgId = {2}, @UserId = {3}",
                batchId, fromOrgId, toOrgId, userId);
        }

        public async Task<IEnumerable<DrugTracker.Models.DTOs.DistributorDashboardRow>> GetDistributorDashboardDataAsync(int orgId)
        {
            return await _context.DistributorDashboardData
                .FromSqlRaw("EXEC [HealthCare].[sp_GetDistributorDashboardData] @DistributorOrgId = {0}", orgId)
                .ToListAsync();
        }

        public async Task DispatchToPharmacySPAsync(string batchId, int fromOrgId, int toOrgId, int userId)
        {
            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_DispatchBatchToPharmacy] @BatchId = {0}, @FromOrgId = {1}, @ToOrgId = {2}, @UserId = {3}",
                batchId, fromOrgId, toOrgId, userId);
        }

        public async Task<IEnumerable<DrugTracker.Models.DTOs.PharmacyDashboardRow>> GetPharmacyDashboardDataAsync(int orgId)
        {
            return await _context.PharmacyDashboardData
                .FromSqlRaw("EXEC [HealthCare].[sp_GetPharmacyDashboardData] @PharmacyOrgId = {0}", orgId)
                .ToListAsync();
        }

        public async Task AcceptBatchAtPharmacySPAsync(string batchId, int pharmacyOrgId, int userId)
        {
            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_AcceptBatchAtPharmacy] @BatchId = {0}, @PharmacyOrgId = {1}, @UserId = {2}",
                batchId, pharmacyOrgId, userId);
        }

        public async Task<IEnumerable<DrugTracker.Models.DTOs.PharmacyInventoryRow>> GetPharmacyInventoryDataAsync(int orgId)
        {
            return await _context.PharmacyInventoryData
                .FromSqlRaw("EXEC [HealthCare].[sp_GetPharmacyInventoryData] @PharmacyOrgId = {0}", orgId)
                .ToListAsync();
        }

        public async Task SellDrugAtPharmacySPAsync(string batchId, int pharmacyOrgId, int quantity, int userId)
        {
            await _context.SetSessionContextAsync();
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [HealthCare].[sp_SellDrugAtPharmacy] @BatchId = {0}, @PharmacyOrgId = {1}, @QuantityToSell = {2}, @UserId = {3}",
                batchId, pharmacyOrgId, quantity, userId);
        }
    }
}
