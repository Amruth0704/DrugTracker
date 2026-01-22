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
            var batches = await _batchRepository.GetIncomingDispatchesAsync(orgId);
            
            // We need list of Pharmacies for the dispatch modal
            ViewBag.Pharmacies = await _context.Organizations.Where(o => o.OrgType == "PHARMACY").ToListAsync();
            
            var viewModels = new List<BatchViewModel>();
            foreach (var b in batches)
            {
                var history = await _batchRepository.GetOwnershipHistoryAsync(b.DrugBatchId);
                var last = history.LastOrDefault();
                // Enabled if last action was transfer TO this distributor (meaning we hold it) 
                // AND we haven't transferred it yet.
                // Simplified: If last action is "TRANSFERRED" and ToOrg == Us, it's ours.
                // If we transferred it, last action would be "TRANSFERRED" and FromOrg == Us.
                
                bool isOurs = last != null && last.ToOrgId == orgId && last.ActionType == "TRANSFERRED";
                
                string transferredTo = "N/A";
                // If we transferred it (ActionType == TRANSFERRED and FromOrg == Us), find out who we sent it to
                // Wait, if LastAction is TRANSFERRED, checks depend on who is viewing.
                // If distributor is viewing:
                // 1. They received it (Status: Transferred, To: Dist) -> Transferred To: N/A (Previous owner handled elsewhere)
                // 2. They sent it (Status: Transferred, From: Dist) -> Transferred To: Pharmacy
                
                if (last != null && last.ActionType == "TRANSFERRED" && last.FromOrgId == orgId)
                {
                    var toOrg = await _context.Organizations.FindAsync(last.ToOrgId);
                    transferredTo = toOrg?.OrgName ?? "Unknown";
                }

                var vm = new BatchViewModel
                {
                    Batch = b,
                    LatestAction = last?.ActionType ?? "N/A",
                    IsActionEnabled = isOurs,
                    TransferredToOrgName = transferredTo
                };
                viewModels.Add(vm);
            }

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
