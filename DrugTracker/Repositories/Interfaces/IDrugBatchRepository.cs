using DrugTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IDrugBatchRepository
    {
        Task<DrugBatch?> GetByBatchIdAsync(string batchId);
        Task<IEnumerable<DrugBatch>> GetBatchesByManufacturerAsync(int orgId);
        Task<IEnumerable<DrugBatch>> GetIncomingDispatchesAsync(int toOrgId);
        Task<string?> GetDrugNameByIdAsync(int drugId);
        Task<int> GetBatchCountForDrugAsync(int drugId);
        Task AddAsync(DrugBatch batch);
        Task UpdateAsync(DrugBatch batch); // Only for status updates if needed, though strictly we used Ledger for state
        Task AddDispatchRecordAsync(BatchOwnershipHistory history);
        Task<IEnumerable<BatchOwnershipHistory>> GetOwnershipHistoryAsync(string batchId);
    }
}
