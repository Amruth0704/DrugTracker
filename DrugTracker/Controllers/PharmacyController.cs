using DrugTracker.Models;
using DrugTracker.Models.DTOs;
using DrugTracker.Models.ViewModels;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DrugTracker.Controllers
{
    [Authorize(Roles = "Pharmacy", AuthenticationSchemes = "PharmacyScheme")]
    public class PharmacyController : Controller
    {
        private readonly IBatchService _batchService;
        private readonly IDrugBatchRepository _batchRepository;

        public PharmacyController(IBatchService batchService, IDrugBatchRepository batchRepository)
        {
            _batchService = batchService;
            _batchRepository = batchRepository;
        }

        public async Task<IActionResult> Dashboard()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            
            // Fetch consolidated data using the new Stored Procedure
            var dashboardData = await _batchRepository.GetPharmacyDashboardDataAsync(orgId);
            
            var viewModels = dashboardData.Select(d => new BatchViewModel
            {
                Batch = new DrugBatch 
                { 
                    DrugBatchId = d.DrugBatchId,
                    Drug = new Drug { DrugName = d.DrugName },
                    QuantityProduced = d.QuantityProduced,
                    ManufactureDate = d.ManufactureDate,
                    ExpiryDate = d.ExpiryDate,
                    CreatedAt = d.CreatedAt
                },
                LatestAction = d.LatestAction ?? "N/A",
                IsActionEnabled = true // SP only returns those that ARE actionable (Incoming)
            }).ToList();

            // Sorting Logic: Most recent first
            viewModels = viewModels
                .OrderByDescending(vm => vm.Batch.CreatedAt)
                .ToList();

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

        public async Task<IActionResult> Inventory()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            
            // Use Stored Procedure for Inventory
            var inventory = await _batchRepository.GetPharmacyInventoryDataAsync(orgId);
            
            var viewModels = inventory.Select(i => new InventoryViewModel
            {
                DrugBatchId = i.DrugBatchId,
                DrugName = i.DrugName,
                AvailableQty = i.AvailableQty,
                ReceivedQty = i.ReceivedQty,
                QuantitySold = i.ReceivedQty - i.AvailableQty, 
                ExpiryDate = i.ExpiryDate
            }).ToList();

            return View(viewModels);
        }

        public async Task<IActionResult> Sales()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            
            // Use SP to get only sellable items
            var inventory = await _batchRepository.GetPharmacyInventoryDataAsync(orgId);
            
            var sellable = inventory.Where(i => i.AvailableQty > 0).Select(i => new InventoryViewModel
            {
                DrugBatchId = i.DrugBatchId,
                DrugName = i.DrugName,
                AvailableQty = i.AvailableQty,
                ExpiryDate = i.ExpiryDate
            }).ToList();
            
            return View(sellable);
        }

        [HttpPost]
        public async Task<IActionResult> RecordSale(SalesViewModel model)
        {
            if (ModelState.IsValid)
            {
                 try
                 {
                     int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                     int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                     
                     // SP validates stock internally
                     await _batchService.SellDrugAsync(model.DrugBatchId, orgId, model.QuantityToSell, userId);
                     TempData["Message"] = "Sale recorded successfully.";
                     return RedirectToAction("Sales");
                 }
                 catch(Exception ex)
                 {
                     TempData["Error"] = ex.Message;
                 }
            }
            return RedirectToAction("Sales");
        }

        // Keep old Sell just in case, or remove. Instructions say "Update". 
        // I will remove the old simple "Sell" method to enforce Sales page usage, or redirect it.
    }
}
