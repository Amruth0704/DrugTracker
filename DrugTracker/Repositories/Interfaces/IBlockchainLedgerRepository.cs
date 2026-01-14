using DrugTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IBlockchainLedgerRepository
    {
        Task AddEntryAsync(BlockchainLedger entry);
        Task<IEnumerable<BlockchainLedger>> GetLedgerByBatchIdAsync(string batchId);
        Task<bool> VerifyLedgerAsync(); // Placeholder for SP call
    }
}
