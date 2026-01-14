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
            
            // Section 1: Incoming batches (Dispatched to here)
            var incoming = await _batchRepository.GetIncomingDispatchesAsync(orgId);
            ViewBag.Incoming = incoming;
            
            // Section 2: Current Inventory
            var inventory = await _inventoryRepository.GetPharmacyInventoryAsync(orgId);
            
            return View(inventory);
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
