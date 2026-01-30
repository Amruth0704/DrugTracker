using DrugTracker.Data;
using DrugTracker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DrugTrackerDbContext _context;
        private IDrugBatchRepository? _drugBatches;
        private IUserRepository? _users;
        private IBlockchainLedgerRepository? _ledger;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(DrugTrackerDbContext context)
        {
            _context = context;
        }

        public IDrugBatchRepository DrugBatches => _drugBatches ??= new DrugBatchRepository(_context);
        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IBlockchainLedgerRepository Ledger => _ledger ??= new BlockchainLedgerRepository(_context);

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                return;
            }
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await SaveChangesAsync();
                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
