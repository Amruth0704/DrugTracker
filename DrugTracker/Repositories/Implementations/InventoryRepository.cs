using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Implementations
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly DrugTrackerDbContext _context;

        public InventoryRepository(DrugTrackerDbContext context)
        {
            _context = context;
        }

        /* WRITE */
        public async Task AddOrUpdateInventoryAsync(Inventory inventory, int userId, int orgId, string role, string ipAddress, bool isManualUpdate = false)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_AddOrUpdateInventory
                    @DrugBatchId,
                    @PharmacyOrgId,
                    @AvailableQty,
                    @ReceivedQty,
                    @IsManualUpdate,
                    @UserId,
                    @OrgId,
                    @Role,
                    @IPAddress",
                new SqlParameter("@DrugBatchId", inventory.DrugBatchId),
                new SqlParameter("@PharmacyOrgId", inventory.PharmacyOrgId),
                new SqlParameter("@AvailableQty", inventory.AvailableQty),
                new SqlParameter("@ReceivedQty", inventory.ReceivedQty),
                new SqlParameter("@IsManualUpdate", isManualUpdate),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@OrgId", orgId),
                new SqlParameter("@Role", role),
                new SqlParameter("@IPAddress", ipAddress)
            );
        }

        public async Task<Inventory?> GetInventoryItemAsync(int pharmacyId, int drugId, string batchId)
        {
            return await _context.Inventories
                .Include(i => i.DrugBatch)
                .ThenInclude(b => b.Drug)
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.PharmacyOrgId == pharmacyId &&
                    i.DrugBatchId == batchId);
        }

        public async Task<IEnumerable<Inventory>> GetPharmacyInventoryAsync(int pharmacyId)
        {
            return await _context.Inventories
                .Include(i => i.DrugBatch)
                .ThenInclude(b => b.Drug)
                .Where(i => i.PharmacyOrgId == pharmacyId && i.AvailableQty > 0)
                .AsNoTracking()
                .ToListAsync();
        }

    }
}
