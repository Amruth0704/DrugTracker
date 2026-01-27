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

            // 1. Validate Batch Creation
            var createdLedger = ledger.FirstOrDefault(l => l.Action == "BATCH_CREATED");
            if (createdLedger != null)
            {
                if (batch.QuantityProduced != createdLedger.Quantity)
                {
                    isTampered = true;
                    validationErrors.Add($"Batch Quantity Mismatch! Current: {batch.QuantityProduced}, Ledger: {createdLedger.Quantity}");
                }
                if (batch.CreatedByOrgId != createdLedger.FromOrgId)
                {
                    isTampered = true;
                    validationErrors.Add("Batch Manufacturer Mismatch!");
                }
            }

            // 2. Validate Pharmacy Inventory (if applicable)
            var acceptedLedger = ledger.FirstOrDefault(l => l.Action == "ACCEPTED_BY_PHARMACY");
            if (acceptedLedger != null && acceptedLedger.ToOrgId.HasValue)
            {
                var inventory = await _inventoryRepository.GetInventoryItemAsync(acceptedLedger.ToOrgId.Value, 0, batchId);
                if (inventory != null)
                {
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
