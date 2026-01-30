using DrugTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IDrugBatchRepository
    {
        Task<DrugBatch?> GetByBatchIdAsync(string batchId);
        Task<IEnumerable<BatchOwnershipHistory>> GetOwnershipHistoryAsync(string batchId);
        
        // Stored Procedure Methods
        Task<string> CreateBatchSPAsync(int drugId, int quantity, DateTime mfgDate, DateTime expDate, int orgId, int userId);
        Task<IEnumerable<DrugTracker.Models.DTOs.ManufacturerDashboardRow>> GetManufacturerDashboardDataAsync(int orgId);
        Task UpdateManufacturerBatchAsync(string batchId, int drugId, int quantity, DateTime mfgDate, DateTime expDate, int orgId);
        Task DeleteManufacturerBatchAsync(string batchId, int orgId);
        Task DispatchToDistributorSPAsync(string batchId, int fromOrgId, int toOrgId, int userId);
        Task<IEnumerable<DrugTracker.Models.DTOs.DistributorDashboardRow>> GetDistributorDashboardDataAsync(int orgId);
        Task DispatchToPharmacySPAsync(string batchId, int fromOrgId, int toOrgId, int userId);
        Task<IEnumerable<DrugTracker.Models.DTOs.PharmacyDashboardRow>> GetPharmacyDashboardDataAsync(int orgId);
        Task AcceptBatchAtPharmacySPAsync(string batchId, int pharmacyOrgId, int userId);
        Task<IEnumerable<DrugTracker.Models.DTOs.PharmacyInventoryRow>> GetPharmacyInventoryDataAsync(int orgId);
        Task SellDrugAtPharmacySPAsync(string batchId, int pharmacyOrgId, int quantity, int userId);
    }
}
