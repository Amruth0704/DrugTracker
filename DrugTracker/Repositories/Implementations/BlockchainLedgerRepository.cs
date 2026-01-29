using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
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

        /* WRITE (APPEND ONLY) */
        public async Task AddEntryAsync(BlockchainLedger entry)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_AddBlockchainLedgerEntry
                    @DrugBatchId,
                    @Action,
                    @FromOrgId,
                    @ToOrgId,
                    @Quantity",
                new SqlParameter("@DrugBatchId", entry.DrugBatchId),
                new SqlParameter("@Action", entry.Action),
                new SqlParameter("@FromOrgId", (object?)entry.FromOrgId ?? DBNull.Value),
                new SqlParameter("@ToOrgId", (object?)entry.ToOrgId ?? DBNull.Value),
                new SqlParameter("@Quantity", (object?)entry.Quantity ?? DBNull.Value)
            );
        }

        /* READ */
        public async Task<IEnumerable<BlockchainLedger>> GetLedgerByBatchIdAsync(string batchId)
        {
            return await _context.BlockchainLedgers
                .FromSqlRaw(
                    @"SELECT *
                      FROM HealthCare.vw_BlockchainLedger_ByBatch
                      WHERE DrugBatchId = @DrugBatchId
                      ORDER BY ActionTime",
                    new SqlParameter("@DrugBatchId", batchId))
                .AsNoTracking()
                .ToListAsync();
        }

        /* VERIFY */
        public async Task<bool> VerifyLedgerAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sys.sp_verify_database_ledger"
                );
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
