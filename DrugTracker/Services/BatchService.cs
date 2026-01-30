using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrugTracker.Services
{
    public class BatchService : IBatchService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BatchService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> CreateBatchAsync(int drugId, int quantity, DateTime mfgDate, DateTime expDate, int orgId, int userId)
        {
            // Now handled entirely by SP (Generates ID + Logs History + Records Ledger)
            return await _unitOfWork.DrugBatches.CreateBatchSPAsync(drugId, quantity, mfgDate, expDate, orgId, userId);
        }

        public async Task UpdateBatchAsync(DrugBatch batch, int userId)
        {
            // Now handled entirely by Stored Procedure (including Blockchain Ledger)
            await _unitOfWork.DrugBatches.UpdateManufacturerBatchAsync(
                batch.DrugBatchId, 
                batch.DrugId, 
                batch.QuantityProduced, 
                batch.ManufactureDate, 
                batch.ExpiryDate, 
                batch.CreatedByOrgId);
        }

        public async Task DeleteBatchAsync(string batchId, int orgId)
        {
            // Now handled by SP (Checks ownership + Records in Ledger + Deletes Batch)
            await _unitOfWork.DrugBatches.DeleteManufacturerBatchAsync(batchId, orgId);
        }

        public async Task DispatchToDistributorAsync(string batchId, int fromOrgId, int toOrgId, int userId)
        {
            // Now handled entirely by SP (Validates only 1 transfer + Records History + Records Ledger)
            await _unitOfWork.DrugBatches.DispatchToDistributorSPAsync(batchId, fromOrgId, toOrgId, userId);
        }

        public async Task DispatchToPharmacyAsync(string batchId, int fromOrgId, int toOrgId, int userId)
        {
            // Now handled entirely by SP (Validates ownership + Records History + Records Ledger)
            await _unitOfWork.DrugBatches.DispatchToPharmacySPAsync(batchId, fromOrgId, toOrgId, userId);
        }

        public async Task AcceptBatchAtPharmacyAsync(string batchId, int pharmacyOrgId, int userId)
        {
            // Now handled entirely by SP (Validates only 1 accept + Updates Inventory + Records History + Records Ledger)
            await _unitOfWork.DrugBatches.AcceptBatchAtPharmacySPAsync(batchId, pharmacyOrgId, userId);
        }

        public async Task SellDrugAsync(string batchId, int pharmacyOrgId, int quantity, int userId)
        {
            // Now handled entirely by SP (Validates stock + Updates Inventory + Records Ledger)
            await _unitOfWork.DrugBatches.SellDrugAtPharmacySPAsync(batchId, pharmacyOrgId, quantity, userId);
        }
    }
}
