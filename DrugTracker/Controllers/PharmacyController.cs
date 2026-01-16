using DrugTracker.Models.ViewModels;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DrugTracker.Controllers
{
    [Authorize(Roles = "Pharmacy")]
    public class PharmacyController : Controller
    {
        private readonly IBatchService _batchService;
        private readonly IDrugBatchRepository _batchRepository;
        private readonly IInventoryRepository _inventoryRepository;

        public PharmacyController(IBatchService batchService, IDrugBatchRepository batchRepository, IInventoryRepository inventoryRepository)
        {
            _batchService = batchService;
            _batchRepository = batchRepository;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<IActionResult> Dashboard()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            var batches = await _batchRepository.GetIncomingDispatchesAsync(orgId);
            
            var viewModels = new List<BatchViewModel>();
            foreach (var b in batches)
            {
                var history = await _batchRepository.GetOwnershipHistoryAsync(b.DrugBatchId);
                var last = history.LastOrDefault();
                
                // Enabled if we haven't accepted it yet.
                // If we accepted it, last action is RECEIVED.
                bool alreadyAccepted = last != null && (last.ActionType == "RECEIVED" || last.ActionType == "ACCEPTED");
                
                var vm = new BatchViewModel
                {
                    Batch = b,
                    LatestAction = last?.ActionType ?? "N/A",
                    IsActionEnabled = !alreadyAccepted
                };
                viewModels.Add(vm);
            }

            // Note: The View expects BatchViewModel list for the top table. 
            // The Inventory list is separate. 
            // We might need a Composite ViewModel if we want both, 
            // but the corrected View logic (Step 340) only iterates Model for Incoming.
            // It has a link "View Full Inventory" instead of a table.
            
            return View(viewModels);
        }

        [HttpPost]
        public async Task<IActionResult> Accept(string batchId)
        {
             try
             {
                 int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                 int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                 await _batchService.AcceptBatchAtPharmacyAsync(batchId, orgId, userId);
                 TempData["Message"] = "Batch accepted into Inventory";
             }
             catch(Exception ex)
             {
                 TempData["Error"] = ex.Message;
             }
             return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Sell(string batchId, int quantity)
        {
             try
             {
                 int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                 int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                 await _batchService.SellDrugAsync(batchId, orgId, quantity, userId);
                 TempData["Message"] = "Drug sold successfully";
             }
             catch(Exception ex)
             {
                 TempData["Error"] = ex.Message;
             }
             return RedirectToAction("Dashboard");
        }
    }
}
