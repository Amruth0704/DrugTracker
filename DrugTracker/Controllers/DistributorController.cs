using DrugTracker.Models;
using DrugTracker.Models.DTOs;
using DrugTracker.Models.ViewModels;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DrugTracker.Controllers
{
    [Authorize(Roles = "Distributor", AuthenticationSchemes = "DistributorScheme")]
    public class DistributorController : Controller
    {
        private readonly IBatchService _batchService;
        private readonly IDrugBatchRepository _batchRepository;
        private readonly DrugTracker.Data.DrugTrackerDbContext _context;

        public DistributorController(IBatchService batchService, IDrugBatchRepository batchRepository, DrugTracker.Data.DrugTrackerDbContext context)
        {
            _batchService = batchService;
            _batchRepository = batchRepository;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            
            // Fetch consolidated data using the new Stored Procedure
            var dashboardData = await _batchRepository.GetDistributorDashboardDataAsync(orgId);
            
            // We need list of Pharmacies for the dispatch modal
            ViewBag.Pharmacies = await _context.Organizations
                .Where(o => o.OrgType == "PHARMACY")
                .ToListAsync();

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
                // Enabled if latest entry's ToOrgId is US and it's a transfer OR if it's still with previous owner (N/A)
                // Actually the SP returns LatestToOrgId which makes this easy:
                IsActionEnabled = (d.LatestToOrgId == orgId && d.LatestAction == "TRANSFERRED"),
                TransferredToOrgName = d.TransferredToOrgName ?? "N/A"
            }).ToList();

            // Sort: Actionable items first, then by Creation Date
            viewModels = viewModels
                .OrderByDescending(vm => vm.IsActionEnabled)
                .ThenByDescending(vm => vm.Batch.CreatedAt)
                .ToList();

            return View(viewModels);
        }

        [HttpPost]
        public async Task<IActionResult> Dispatch(string batchId, int pharmacyId)
        {
             try
             {
                 int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                 int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                 await _batchService.DispatchToPharmacyAsync(batchId, orgId, pharmacyId, userId);
                 TempData["Message"] = "Batch dispatched to Pharmacy successfully";
             }
             catch(Exception ex)
             {
                 TempData["Error"] = ex.Message;
             }
             return RedirectToAction("Dashboard");
        }
    }
}
