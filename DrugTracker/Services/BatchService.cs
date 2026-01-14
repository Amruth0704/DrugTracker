using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DrugTracker.Services
{
    public interface IBatchService
    {
        Task<DrugBatch> CreateBatchAsync(int drugId, int quantity, DateTime manufacturingDate, DateTime expiryDate, int manufOrgId, int userId);
        Task DispatchToDistributorAsync(string batchId, int fromOrgId, int toOrgId, int userId);
        Task DispatchToPharmacyAsync(string batchId, int fromOrgId, int toOrgId, int userId);
        Task AcceptBatchAtPharmacyAsync(string batchId, int pharmacyOrgId, int userId);
        Task SellDrugAsync(string batchId, int pharmacyOrgId, int quantity, int userId);
    }

    public class BatchService : IBatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlockchainService _blockchainService;

        public BatchService(IUnitOfWork unitOfWork, IBlockchainService blockchainService)
        {
            _unitOfWork = unitOfWork;
            _blockchainService = blockchainService;
        }

        public async Task<DrugBatch> CreateBatchAsync(int drugId, int quantity, DateTime manufacturingDate, DateTime expiryDate, int manufOrgId, int userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Auto-ID Generation Logic
                // 1. Get Drug Name
                var drug = await _unitOfWork.DrugBatches.GetByBatchIdAsync("DUMMY"); // Hack to get repository context if needed or just use context directly? 
                // Repository abstraction limits direct access to DbContext for simple lookup unless we add method. 
                // We don't have GetDrugById in DrugBatchRepository. We assume we can get it or we add it to UnitOfWork/Repo.
                // Let's add specific logic here. Ideally we need Drug Repository.
                // Since I didn't create DrugRepository, I'll assume we can pass DrugName or I should add Generic Get or specific GetDrug.
                // I will add a quick GetDrugById to IDrugBatchRepository? No, that belongs to DrugRepository.
                // For now, I'll assume I can get it via a raw query or just adding a method to UnitOfWork. 
                // Wait, I can't easily get Drug Name without a repo.
                // I will use _unitOfWork.DrugBatches to access DbContext? No, encapsulated.
                // I'll add `GetDrugByIdAsync` to `IDrugBatchRepository` for now as a helper, or assume passed in.
                
                // Correction: adding `GetDrugByIdAsync` is cleaner. I'll modify IDrugBatchRepository later? 
                // Or I can just query the DB if I had access.
                // Let's assume for this step I'll update the Interface in a moment.
                // For now, I'll write the logic assuming the method exists.
                
                // Wait, I can use a simpler approach:
                // Just use the prefix "DRUG" + ID if name is creating friction, but requirements say "Derived from DrugName".
                
                // Let's rely on `_unitOfWork` exposing `DbContext`? No, that breaks pattern.
                // I should create `IDrugRepository`? Overkill?
                // I'll add `GetDrugNameAsync(int drugId)` to `IDrugBatchRepository` to keep it simple.
                
                // Actually, I'll proceed creating this file assuming `GetDrugByIdAsync` exists in `IDrugBatchRepository` 
                // and then I will immediately update the Repository/Interface to include it.
                
                var drugName = await _unitOfWork.DrugBatches.GetDrugNameByIdAsync(drugId); 
                if (string.IsNullOrEmpty(drugName)) throw new Exception("Drug not found");

                string prefix = drugName.Length >= 3 ? drugName.Substring(0, 3).ToUpper() : drugName.ToUpper();
                
                // Get count of batches for this drug to determine suffix
                // We need a method `GetBatchCountForDrugAsync`.
                int count = await _unitOfWork.DrugBatches.GetBatchCountForDrugAsync(drugId);
                string batchId = $"{prefix}{(count + 1):D3}";

                var batch = new DrugBatch
                {
                    DrugBatchId = batchId,
                    DrugId = drugId,
                    QuantityProduced = quantity,
                    ManufactureDate = manufacturingDate,
                    ExpiryDate = expiryDate,
                    CreatedByOrgId = manufOrgId
                };

                await _unitOfWork.DrugBatches.AddAsync(batch);
                
                // Initial History
                var history = new BatchOwnershipHistory
                {
                    DrugBatchId = batchId,
                    ActionType = "CREATED",
                    PerformedBy = userId,
                    ToOrgId = manufOrgId, 
                    ActionTime = DateTime.Now
                };
                 await _unitOfWork.DrugBatches.AddDispatchRecordAsync(history);

                // Blockchain
                await _blockchainService.RecordActionAsync(batchId, "BATCH_CREATED", manufOrgId, null, quantity);

                await _unitOfWork.CommitTransactionAsync();
                return batch;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task DispatchToDistributorAsync(string batchId, int fromOrgId, int toOrgId, int userId)
        {
             await _unitOfWork.BeginTransactionAsync();
             try
             {
                 await _blockchainService.RecordActionAsync(batchId, "TRANSFER_TO_DISTRIBUTOR", fromOrgId, toOrgId, null);
                 
                 var history = new BatchOwnershipHistory
                 {
                     DrugBatchId = batchId,
                     ActionType = "TRANSFERRED",
                     ToOrgId = toOrgId,
                     FromOrgId = fromOrgId,
                     PerformedBy = userId,
                     ActionTime = DateTime.Now
                 };
                 await _unitOfWork.DrugBatches.AddDispatchRecordAsync(history);
                 
                 await _unitOfWork.CommitTransactionAsync();
             }
             catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }

        public async Task DispatchToPharmacyAsync(string batchId, int fromOrgId, int toOrgId, int userId)
        {
             await _unitOfWork.BeginTransactionAsync();
             try
             {
                 await _blockchainService.RecordActionAsync(batchId, "TRANSFER_TO_PHARMACY", fromOrgId, toOrgId, null);
                 
                 var history = new BatchOwnershipHistory
                 {
                     DrugBatchId = batchId,
                     ActionType = "TRANSFERRED",
                     ToOrgId = toOrgId,
                     FromOrgId = fromOrgId,
                     PerformedBy = userId,
                     ActionTime = DateTime.Now
                 };
                 await _unitOfWork.DrugBatches.AddDispatchRecordAsync(history);
                 
                 await _unitOfWork.CommitTransactionAsync();
             }
             catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }

        public async Task AcceptBatchAtPharmacyAsync(string batchId, int pharmacyOrgId, int userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Add to Inventory
                var batch = await _unitOfWork.DrugBatches.GetByBatchIdAsync(batchId);
                if (batch == null) throw new Exception("Batch not found");

                var inventory = new Inventory
                {
                    PharmacyOrgId = pharmacyOrgId,
                    // DrugId = batch.DrugId, // Not in model
                    DrugBatchId = batchId,
                    AvailableQty = batch.QuantityProduced,
                    LastUpdated = DateTime.Now
                };
                await _unitOfWork.Inventory.AddOrUpdateInventoryAsync(inventory);

                await _blockchainService.RecordActionAsync(batchId, "ACCEPTED_BY_PHARMACY", null, pharmacyOrgId, batch.QuantityProduced);
                
                 var history = new BatchOwnershipHistory
                 {
                     DrugBatchId = batchId,
                     ActionType = "RECEIVED",
                     ToOrgId = pharmacyOrgId,
                     PerformedBy = userId,
                     ActionTime = DateTime.Now
                 };
                 await _unitOfWork.DrugBatches.AddDispatchRecordAsync(history);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }

        public async Task SellDrugAsync(string batchId, int pharmacyOrgId, int quantity, int userId)
        {
             await _unitOfWork.BeginTransactionAsync();
            try
            {
                var batch = await _unitOfWork.DrugBatches.GetByBatchIdAsync(batchId);
                if(batch == null) throw new Exception("Batch not found");

                // Now get inventory
                var inv = await _unitOfWork.Inventory.GetInventoryItemAsync(pharmacyOrgId, batch.DrugId, batchId);
                if (inv == null || inv.AvailableQty < quantity) throw new Exception("Insufficient inventory");

                inv.AvailableQty -= quantity;
                await _unitOfWork.Inventory.AddOrUpdateInventoryAsync(inv);

                await _blockchainService.RecordActionAsync(batchId, "SOLD_TO_CONSUMER", pharmacyOrgId, null, quantity);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }
    }
}
