using DrugTracker.Models;
using System;
using System.Threading.Tasks;

namespace DrugTracker.Services
{
    public interface IBatchService
    {
        Task<string> CreateBatchAsync(int drugId, int quantity, DateTime mfgDate, DateTime expDate, int orgId, int userId);
        Task UpdateBatchAsync(DrugBatch batch, int userId);
        Task DeleteBatchAsync(string batchId, int orgId);
        Task DispatchToDistributorAsync(string batchId, int fromOrgId, int toOrgId, int userId);
        Task DispatchToPharmacyAsync(string batchId, int fromOrgId, int toOrgId, int userId);
        Task AcceptBatchAtPharmacyAsync(string batchId, int pharmacyOrgId, int userId);
        Task SellDrugAsync(string batchId, int pharmacyOrgId, int quantity, int userId);
    }
}
