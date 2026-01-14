using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using System.Threading.Tasks;

namespace DrugTracker.Services
{
    public interface IBlockchainService
    {
        Task RecordActionAsync(string batchId, string action, int? fromOrgId, int? toOrgId, int? quantity);
    }

    public class BlockchainService : IBlockchainService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlockchainService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RecordActionAsync(string batchId, string action, int? fromOrgId, int? toOrgId, int? quantity)
        {
            var entry = new BlockchainLedger
            {
                DrugBatchId = batchId,
                Action = action,
                FromOrgId = fromOrgId,
                ToOrgId = toOrgId,
                Quantity = quantity,
                ActionTime = System.DateTime.Now
            };

            await _unitOfWork.Ledger.AddEntryAsync(entry);
            // Verify? In a real system we might re-hash the chain here.
        }
    }
}
