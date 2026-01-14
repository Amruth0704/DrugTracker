using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Implementations
{
    public class BlockchainLedgerRepository : IBlockchainLedgerRepository
    {
        private readonly DrugTrackerDbContext _context;

        public BlockchainLedgerRepository(DrugTrackerDbContext context)
        {
            _context = context;
        }

        public async Task AddEntryAsync(BlockchainLedger entry)
        {
            await _context.BlockchainLedgers.AddAsync(entry);
        }

        public async Task<IEnumerable<BlockchainLedger>> GetLedgerByBatchIdAsync(string batchId)
        {
            return await _context.BlockchainLedgers
                .Where(l => l.DrugBatchId == batchId)
                .OrderBy(l => l.ActionTime) // Enforce chronological order
                .ToListAsync();
        }

        public async Task<bool> VerifyLedgerAsync()
        {
            // Execute the system stored procedure to verify the ledger
            // Note: This requires the DB to be actually set up as a Ledger DB.
            try 
            {
                await _context.Database.ExecuteSqlRawAsync("EXEC sys.sp_verify_database_ledger");
                return true;
            }
            catch
            {
                // In case of error (tampering detected or not supported), return false
                return false;
            }
        }
    }
}
