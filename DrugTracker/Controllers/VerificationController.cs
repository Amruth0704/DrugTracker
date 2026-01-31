using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DrugTracker.Controllers
{
    public class VerificationController : Controller
    {
        private readonly IDrugBatchRepository _batchRepository;
        private readonly IBlockchainLedgerRepository _ledgerRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IQrCodeService _qrCodeService;

        public VerificationController(IDrugBatchRepository batchRepository, 
            IBlockchainLedgerRepository ledgerRepository, 
            IInventoryRepository inventoryRepository,
            IQrCodeService qrCodeService)
        {
            _batchRepository = batchRepository;
            _ledgerRepository = ledgerRepository;
            _inventoryRepository = inventoryRepository;
            _qrCodeService = qrCodeService;
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

            // 3. Validate Pharmacy Inventory & Sale Status
            var acceptedLedger = ledger.FirstOrDefault(l => l.Action == "ACCEPTED_BY_PHARMACY");
            var soldLedger = ledger.FirstOrDefault(l => l.Action == "SOLD_TO_CONSUMER" || l.Action == "SOLD");
            
            if (acceptedLedger == null)
            {
                isTampered = true;
                validationErrors.Add("Tablet might be fake because its never reached the Pharmacy");
            }
            else if (soldLedger == null)
            {
                // User requirement: "The sale not yet started by pharmacy might be fake drug Reconfirm with Pharmacy"
                isTampered = true; 
                validationErrors.Add("The sale not yet started by pharmacy might be fake drug Reconfirm with Pharmacy");
            }
            else if (acceptedLedger.ToOrgId.HasValue)
            {
                var inventory = await _inventoryRepository.GetInventoryItemAsync(acceptedLedger.ToOrgId.Value, 0, batchId);
                if (inventory != null)
                {
                   if (inventory.ReceivedQty != acceptedLedger.Quantity)
                   {
                       isTampered = true;
                       validationErrors.Add("Received Quantity tampered");
                   }
                   
                   if (inventory.IsTampered)
                   {
                       isTampered = true;
                       validationErrors.Add("Tampered at Pharmacy (Inventory Quantity Manually Reset)");
                   }

                   // Blockchain Verification: Validate Available Qty against Sales
                   var totalSold = ledger.Where(l => l.Action == "SOLD_TO_CONSUMER" || l.Action == "SOLD")
                                         .Sum(l => l.Quantity ?? 0);
                   
                   // Expected = Accepted - Sold. (Note: using acceptedLedger.Quantity (int?) so coalesce to 0)
                   var expectedAvailable = (acceptedLedger.Quantity ?? 0) - totalSold;

                   if (inventory.AvailableQty != expectedAvailable)
                   {
                       isTampered = true;
                       validationErrors.Add("Available Quantity tampered");
                   }
                }
            }

            ViewBag.History = history;
            ViewBag.Ledger = ledger;
            ViewBag.IsTampered = isTampered;
            ViewBag.ValidationErrors = validationErrors;
            
            return View("Details", batch);
        }

        [HttpGet]
        public async Task<IActionResult> SupplyChain(string batchId)
        {
            if (string.IsNullOrEmpty(batchId)) return RedirectToAction("Index");

            var batch = await _batchRepository.GetByBatchIdAsync(batchId);
            var ledger = await _ledgerRepository.GetLedgerByBatchIdAsync(batchId);

            // Reconstruct Batch if missing locally but exists in ledger
            if (batch == null && ledger != null && ledger.Any())
            {
                var createdEntry = ledger.OrderBy(l => l.ActionTime).FirstOrDefault(l => l.Action == "BATCH_CREATED" || l.Action == "CREATED");
                if (createdEntry != null)
                {
                    batch = new DrugBatch
                    {
                        DrugBatchId = batchId,
                        QuantityProduced = createdEntry.Quantity ?? 0,
                        CreatedByOrgId = createdEntry.FromOrgId ?? 0,
                        ManufactureDate = createdEntry.ActionTime,
                        ExpiryDate = createdEntry.ActionTime.AddYears(3)
                    };
                }
            }

            if (batch == null)
            {
                ViewBag.Error = "Batch not found.";
                return View("Index");
            }

            var history = await _batchRepository.GetOwnershipHistoryAsync(batchId);

            // Reconstruct history from ledger if missing locally
            if ((history == null || !history.Any()) && ledger != null)
            {
                history = ledger.Select(l => new BatchOwnershipHistory
                {
                    DrugBatchId = l.DrugBatchId,
                    ActionType = l.Action == "BATCH_CREATED" ? "CREATED" : l.Action,
                    ActionTime = l.ActionTime,
                    FromOrgId = l.FromOrgId,
                    ToOrgId = l.ToOrgId ?? (l.Action == "BATCH_CREATED" ? (l.FromOrgId ?? 0) : 0),
                    PerformedBy = 0
                }).OrderBy(h => h.ActionTime).ToList();
            }

            ViewBag.History = history;
            return View(batch);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyViaQr(IFormFile qrFile)
        {
            if (qrFile == null || qrFile.Length == 0)
            {
                ViewBag.Error = "Please upload a valid QR code image.";
                return View("Index");
            }

            try
            {
                using (var stream = qrFile.OpenReadStream())
                {
                    string? decodedJson = _qrCodeService.DecodeQrCode(stream);
                    if (string.IsNullOrEmpty(decodedJson))
                    {
                        ViewBag.Error = "Could not read QR code. Please ensure it is a clear image.";
                        return View("Index");
                    }

                    var qrData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(decodedJson);
                    string? batchId = qrData?.GetProperty("Id").GetString();
                    
                    if (string.IsNullOrEmpty(batchId))
                    {
                        ViewBag.Error = "Invalid QR code format.";
                        return View("Index");
                    }

                    return await Track(batchId);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error processing QR code: " + ex.Message;
                return View("Index");
            }
        }
    }
}
