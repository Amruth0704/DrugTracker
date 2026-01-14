using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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

        public async Task AddOrUpdateInventoryAsync(Inventory inventory)
        {
            // We only need to check by BatchId and PharmacyId since Batch implies Drug
            var existing = await _context.Inventories
                .FirstOrDefaultAsync(i => i.PharmacyOrgId == inventory.PharmacyOrgId && i.DrugBatchId == inventory.DrugBatchId);
            
            if (existing != null)
            {
                existing.AvailableQty += inventory.AvailableQty; // Add to existing
                existing.LastUpdated = System.DateTime.Now;
                _context.Inventories.Update(existing);
            }
            else
            {
                await _context.Inventories.AddAsync(inventory);
            }
        }

        public async Task<Inventory?> GetInventoryItemAsync(int pharmacyId, int drugId, string batchId)
        {
            // Ignoring drugId param as batchId is unique enough, or check drug via nav property
            return await _context.Inventories
                .Include(i => i.DrugBatch)
                .ThenInclude(b => b.Drug)
                .FirstOrDefaultAsync(i => i.PharmacyOrgId == pharmacyId && i.DrugBatchId == batchId);
        }

        public async Task<IEnumerable<Inventory>> GetPharmacyInventoryAsync(int pharmacyId)
        {
            return await _context.Inventories
                .Include(i => i.DrugBatch)
                .ThenInclude(b => b.Drug)
                .Where(i => i.PharmacyOrgId == pharmacyId && i.AvailableQty > 0)
                .ToListAsync();
        }
    }
}
