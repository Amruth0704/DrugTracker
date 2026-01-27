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
            // 1. Get the last record to find the PreviousHash
            var lastEntry = (await _unitOfWork.Ledger.GetLedgerByBatchIdAsync(batchId)).LastOrDefault();
            string prevHash = lastEntry?.CurrentHash ?? "0"; // Genesis hash for this batch

            var entry = new BlockchainLedger
            {
                DrugBatchId = batchId,
                Action = action,
                FromOrgId = fromOrgId,
                ToOrgId = toOrgId,
                Quantity = quantity,
                ActionTime = System.DateTime.Now,
                PreviousHash = prevHash
            };

            // 2. Calculate CurrentHash (BatchId + Action + Qty + PrevHash)
            string dataToHash = $"{batchId}{action}{quantity ?? 0}{prevHash}";
            entry.CurrentHash = ComputeHash(dataToHash);

            await _unitOfWork.Ledger.AddEntryAsync(entry);
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return System.BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
