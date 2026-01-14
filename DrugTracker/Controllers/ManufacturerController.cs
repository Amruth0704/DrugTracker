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
    [Authorize(Roles = "Manufacturer")]
    public class ManufacturerController : Controller
    {
        private readonly IBatchService _batchService;
        private readonly IDrugBatchRepository _batchRepository;
        private readonly DrugTracker.Data.DrugTrackerDbContext _context; // Direct context access for dropdowns/lookups if repository doesn't have it

        public ManufacturerController(IBatchService batchService, IDrugBatchRepository batchRepository, DrugTracker.Data.DrugTrackerDbContext context)
        {
            _batchService = batchService;
            _batchRepository = batchRepository;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            var batches = await _batchRepository.GetBatchesByManufacturerAsync(orgId);
            return View(batches);
        }

        [HttpGet]
        public async Task<IActionResult> CreateBatch()
        {
            ViewBag.DrugList = new SelectList(await _context.Drugs.ToListAsync(), "DrugId", "DrugName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBatch(CreateBatchViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                    int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    await _batchService.CreateBatchAsync(model.DrugId, model.Quantity, model.ManufactureDate, model.ExpiryDate, orgId, userId);
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
             ViewBag.DrugList = new SelectList(await _context.Drugs.ToListAsync(), "DrugId", "DrugName");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Dispatch(string batchId, int distributorId)
        {
             try
             {
                 int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                 int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                 
                 // For demo, we might need a way to select distributor. 
                 // If not provided in form, we hardcode or select first.
                 // Assuming passed from a modal or form.
                 if(distributorId == 0)
                 {
                     // Fallback: pick first distributor
                     var dist = await _context.Organizations.Where(o => o.OrgType == "DISTRIBUTOR").FirstOrDefaultAsync();
                     if (dist != null) distributorId = dist.OrgId;
                     else throw new Exception("No Distributor found");
                 }
                 
                 await _batchService.DispatchToDistributorAsync(batchId, orgId, distributorId, userId);
                 TempData["Message"] = "Batch dispatched successfully";
             }
             catch(Exception ex)
             {
                 TempData["Error"] = ex.Message;
             }
             return RedirectToAction("Dashboard");
        }
    }
}
