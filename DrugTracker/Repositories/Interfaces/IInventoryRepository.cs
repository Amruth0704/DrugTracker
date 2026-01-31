using DrugTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IInventoryRepository
    {
        Task<Inventory?> GetInventoryItemAsync(int pharmacyId, int drugId, string batchId);
        Task<IEnumerable<Inventory>> GetPharmacyInventoryAsync(int pharmacyId);
        Task AddOrUpdateInventoryAsync(Inventory inventory, int userId, int orgId, string role, string ipAddress, bool isManualUpdate = false);
    }
}
