using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DrugTracker.Controllers
{
    [Authorize(Roles = "Distributor")]
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
            // Show batches sent to this Distributor
            var batches = await _batchRepository.GetIncomingDispatchesAsync(orgId);
            
            // We need list of Pharmacies for the dispatch modal
            ViewBag.Pharmacies = await _context.Organizations.Where(o => o.OrgType == "PHARMACY").ToListAsync();
            
            return View(batches);
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
