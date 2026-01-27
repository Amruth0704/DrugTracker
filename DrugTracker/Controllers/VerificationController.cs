using DrugTracker.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DrugTracker.Controllers
{
    public class VerificationController : Controller
    {
        private readonly IDrugBatchRepository _batchRepository;
        private readonly IBlockchainLedgerRepository _ledgerRepository;

        private readonly IInventoryRepository _inventoryRepository;

        public VerificationController(IDrugBatchRepository batchRepository, IBlockchainLedgerRepository ledgerRepository, IInventoryRepository inventoryRepository)
        {
            _batchRepository = batchRepository;
            _ledgerRepository = ledgerRepository;
            _inventoryRepository = inventoryRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Track(string batchId)
        {
            if (string.IsNullOrEmpty(batchId)) return RedirectToAction("Index");

            var batch = await _batchRepository.GetByBatchIdAsync(batchId);
            if (batch == null)
            {
                ViewBag.Error = "Batch not found.";
                return View("Index");
            }

            var history = await _batchRepository.GetOwnershipHistoryAsync(batchId);
            var ledger = await _ledgerRepository.GetLedgerByBatchIdAsync(batchId);
            
            // --- VALIDATION LOGIC ---
            var validationErrors = new List<string>();
            bool isTampered = false;

            // 1. Check for Deletion
            var deletedLedger = ledger.FirstOrDefault(l => l.Action == "BATCH_DELETED");
            if (deletedLedger != null)
            {
                isTampered = true;
                validationErrors.Add("BATCH DELETED! This batch has been deleted by the manufacturer and should not exist.");
            }

            // 2. Validate Batch Creation/Edit
            // Find the LATEST authoritative record from Manufacturer (Created OR Edited)
            var authoritativeLedger = ledger
                .Where(l => l.Action == "BATCH_CREATED" || l.Action == "BATCH_EDITED")
                .OrderByDescending(l => l.ActionTime)
                .FirstOrDefault();

            if (authoritativeLedger != null)
            {
                if (batch.QuantityProduced != authoritativeLedger.Quantity)
                {
                    isTampered = true;
                    validationErrors.Add($"Batch Quantity Mismatch! Current: {batch.QuantityProduced}, Ledger ({authoritativeLedger.Action}): {authoritativeLedger.Quantity}");
                }
                if (batch.CreatedByOrgId != authoritativeLedger.FromOrgId)
                {
                    isTampered = true;
                    validationErrors.Add("Batch Manufacturer Mismatch!");
                }
            }
            else
            {
                // If no creation record exists at all?
                 isTampered = true;
                 validationErrors.Add("No Blockchain Creation Record Found!");
            }

            // 3. Validate Pharmacy Inventory (if applicable)

            // 3. Validate Pharmacy Inventory (if applicable)
            var acceptedLedger = ledger.FirstOrDefault(l => l.Action == "ACCEPTED_BY_PHARMACY");
            if (acceptedLedger != null && acceptedLedger.ToOrgId.HasValue)
            {
                var inventory = await _inventoryRepository.GetInventoryItemAsync(acceptedLedger.ToOrgId.Value, 0, batchId);
                if (inventory != null)
                {
                   // Validation: Inventory ReceivedQty must match what was recorded when ACCEPTED.
                   // Note: If batch was edited AFTER acceptance (which shouldn't happen per business rules), 
                   // the Acceptance record remains the truth of what THEY received.
                   if (inventory.ReceivedQty != acceptedLedger.Quantity)
                   {
                       isTampered = true;
                       validationErrors.Add($"Pharmacy Inventory Mismatch! Inventory Received: {inventory.ReceivedQty}, Ledger: {acceptedLedger.Quantity}");
                   }
                }
            }

            ViewBag.History = history;
            ViewBag.Ledger = ledger;
            ViewBag.IsTampered = isTampered;
            ViewBag.ValidationErrors = validationErrors;
            
            return View("Details", batch);
        }
    }
}
