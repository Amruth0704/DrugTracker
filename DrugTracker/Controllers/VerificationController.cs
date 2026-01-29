using DrugTracker.Models;
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

            // Fetch Ledger independent of Batch existence first (Blockchain is authority)
            var ledger = await _ledgerRepository.GetLedgerByBatchIdAsync(batchId);

            // 0. Check if it exists in Blockchain AT ALL
            if (ledger == null || !ledger.Any())
            {
                 ViewBag.Error = "Fake drug never registered in blockchain";
                 return View("Index");
            }

            var batch = await _batchRepository.GetByBatchIdAsync(batchId);
            // Fallback if batch is somehow missing in local DB but likely exists in ledger (though unlikely in current architecture)
            if (batch == null)
            {
                // Reconstruct Batch from Ledger if deleted locally
                var createdEntry = ledger.OrderBy(l => l.ActionTime).FirstOrDefault(l => l.Action == "BATCH_CREATED" || l.Action == "CREATED");
                
                if (createdEntry == null)
                {
                     ViewBag.Error = "Batch record missing from local database, but exists on ledger.";
                     return View("Index");
                }

                // Reconstruct a proxy batch
                batch = new DrugBatch
                {
                    DrugBatchId = batchId,
                    QuantityProduced = createdEntry.Quantity ?? 0,
                    CreatedByOrgId = createdEntry.FromOrgId ?? 0,
                    ManufactureDate = createdEntry.ActionTime, // Approximate
                    ExpiryDate = createdEntry.ActionTime.AddYears(3) // Approximate based on logic
                };

                // Try to fetch Org Name
                 // We need to use _inventoryRepository or access context. Controller has _inventoryRepository which might allow getting context or we inject context?
                 // VerificationController constructor only has repositories. 
                 // Let's assume we can't easily get the Org Name without an Org Repository or Context.
                 // However, the View handles null Org safely (?Model.CreatedByOrg?.OrgName).
                 // We will just leave it as null or try to fetch if we had a repo.
                 // Wait, we can't fetch Org without a repo method.
                 // Let's check what repositories available: inventory.
                 
                 // Ideally we inject IOrganizationRepository or utilize UnitOfWork if available, but currently we inject specific repos.
                 // Let's just proceed with the proxy batch. The View will show empty Manufacturer Name, or we can try to pass it via ViewBag if we really wanted, but for now this suffices to unblock the "Missing" error.
            }

            var history = await _batchRepository.GetOwnershipHistoryAsync(batchId);
            
            // If local history is empty (likely due to cascade delete), reconstruct history from Ledger
            if (history == null || !history.Any())
            {
                history = ledger.Select(l => new BatchOwnershipHistory
                {
                    DrugBatchId = l.DrugBatchId,
                    ActionType = l.Action == "BATCH_CREATED" ? "CREATED" : l.Action, // Map Ledger action to History action
                    ActionTime = l.ActionTime,
                    FromOrgId = l.FromOrgId,
                    ToOrgId = l.ToOrgId ?? (l.Action == "BATCH_CREATED" ? (l.FromOrgId ?? 0) : 0),
                    PerformedBy = 0 // Ledger doesn't track User ID, only Org ID
                }).OrderBy(h => h.ActionTime).ToList();
            }
            
            // --- VALIDATION LOGIC ---
            var validationErrors = new List<string>();
            bool isTampered = false;

            // 1. Check for Deletion
            var deletedLedger = ledger.FirstOrDefault(l => l.Action == "BATCH_DELETED");
            if (deletedLedger != null)
            {
                isTampered = true;
                validationErrors.Add("BATCH DELETED! This batch was created but has been DELETED by the manufacturer.");
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
            var acceptedLedger = ledger.FirstOrDefault(l => l.Action == "ACCEPTED_BY_PHARMACY");
            
            // NEW CHECK: If it hasn't reached pharmacy yet
            if (acceptedLedger == null)
            {
                // User requirement: "Tablet might be fake because its never reached the Pharmacy"
                // We mark it as 'tampered' or just a warning? The prompt implies it's a "fake" warning.
                isTampered = true;
                validationErrors.Add("Tablet might be fake because its never reached the Pharmacy");
            }
            else if (acceptedLedger.ToOrgId.HasValue)
            {
                var inventory = await _inventoryRepository.GetInventoryItemAsync(acceptedLedger.ToOrgId.Value, 0, batchId);
                if (inventory != null)
                {
                   // Validation: Inventory ReceivedQty must match what was recorded when ACCEPTED.
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
