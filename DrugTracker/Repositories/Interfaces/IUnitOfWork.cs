using System;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDrugBatchRepository DrugBatches { get; }
        IUserRepository Users { get; }
        IInventoryRepository Inventory { get; }
        IBlockchainLedgerRepository Ledger { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
