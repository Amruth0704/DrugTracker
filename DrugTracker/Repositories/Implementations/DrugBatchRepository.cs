using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
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

        public async Task AddAsync(DrugBatch batch)
        {
            await _context.DrugBatches.AddAsync(batch);
        }
 
        public async Task UpdateAsync(DrugBatch batch)
        {
            _context.DrugBatches.Update(batch);
            await Task.CompletedTask;
        }
 
        public async Task DeleteAsync(string batchId)
        {
            var batch = await _context.DrugBatches.FindAsync(batchId);
            if (batch != null)
            {
                _context.DrugBatches.Remove(batch);
            }
        }

        public async Task AddDispatchRecordAsync(BatchOwnershipHistory history)
        {
            await _context.BatchOwnershipHistories.AddAsync(history);
        }

        public async Task<DrugBatch?> GetByBatchIdAsync(string batchId)
        {
            return await _context.DrugBatches
                .Include(b => b.Drug)
                .Include(b => b.CreatedByOrg)
                .FirstOrDefaultAsync(b => b.DrugBatchId == batchId);
        }

        public async Task<int> GetBatchCountForDrugAsync(int drugId)
        {
            return await _context.DrugBatches.CountAsync(b => b.DrugId == drugId);
        }

        public async Task<string?> GetDrugNameByIdAsync(int drugId)
        {
             return await _context.Drugs
                .Where(d => d.DrugId == drugId)
                .Select(d => d.DrugName)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<DrugBatch>> GetBatchesByManufacturerAsync(int orgId)
        {
            return await _context.DrugBatches
                .Include(b => b.Drug)
                .Where(b => b.CreatedByOrgId == orgId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DrugBatch>> GetIncomingDispatchesAsync(int toOrgId)
        {
            // Get batches where the *latest* ownership history is directed TO this org
            // This is a bit complex. We want batches where the current holder is this org OR it was dispatched to this org.
            // Simplified: Find history records where ToOrgId == toOrgId.
            // Then group by BatchId and take the latest.
            
            // Actually, a cleaner way for "Incoming Dispatch" depends on the status.
            // If the application state is tracked via Ledger/History, we need to know if it's "Received" or "Pending".
            // For now, let's return all batches where the last history event was a transfer TO this org.
            
            var batchIds = await _context.BatchOwnershipHistories
                .Where(h => h.ToOrgId == toOrgId)
                .Select(h => h.DrugBatchId)
                .Distinct()
                .ToListAsync();

            var batches = await _context.DrugBatches
                .Include(b => b.Drug)
                .Include(b => b.CreatedByOrg)
                .Where(b => batchIds.Contains(b.DrugBatchId))
                .ToListAsync();
                
            return batches;
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

    }
}
