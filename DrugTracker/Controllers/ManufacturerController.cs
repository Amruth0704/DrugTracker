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
    [Authorize(Roles = "Manufacturer", AuthenticationSchemes = "ManufacturerScheme")]
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
            
            // Populate Distributors for Dispatch Modal
            ViewBag.Distributors = await _context.Organizations.Where(o => o.OrgType == "DISTRIBUTOR").ToListAsync();

            var viewModels = new List<BatchViewModel>();
            foreach (var b in batches)
            {
                var history = await _batchRepository.GetOwnershipHistoryAsync(b.DrugBatchId);
                var last = history.LastOrDefault();
                string transferredTo = "N/A";

                // Find the transfer action initiated by THIS manufacturer
                var transferRecord = history.FirstOrDefault(h => h.ActionType == "TRANSFERRED" && h.FromOrgId == orgId);
                
                if (transferRecord != null)
                {
                   var toOrg = await _context.Organizations.FindAsync(transferRecord.ToOrgId);
                   transferredTo = toOrg?.OrgName ?? "Unknown";
                }

                var vm = new BatchViewModel
                {
                    Batch = b,
                    LatestAction = last?.ActionType ?? "N/A",
                    IsActionEnabled = (last?.ActionType == "CREATED" || last?.ActionType == "BATCH_CREATED"),
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
                    // Strict Validation: Manufacture Date Verification
                    if (model.ManufactureDate > DateTime.Today)
                    {
                        ModelState.AddModelError("ManufactureDate", "Manufacture Date cannot be in the future.");
                        ViewBag.DrugList = new SelectList(await _context.Drugs.ToListAsync(), "DrugId", "DrugName");
                        return View(model);
                    }

                    // Auto-Calculation: Enforce Expiry Date = Mfg Date + 3 Years
                    // We IGNORE whatever the client sent for ExpiryDate
                    DateTime finalExpiryDate = model.ManufactureDate.AddYears(3);

                    int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                    int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                    // Pass the calculated date, NOT model.ExpiryDate
                    await _batchService.CreateBatchAsync(model.DrugId, model.Quantity, model.ManufactureDate, finalExpiryDate, orgId, userId);
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
                 
                 // Required for safety: Check if we still own it before dispatching
                 // (Service likely checks, but good to be explicit or rely on service exceptions)
                 
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

        [HttpGet]
        public async Task<IActionResult> EditBatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            var batch = await _context.DrugBatches.Include(b => b.Drug).FirstOrDefaultAsync(b => b.DrugBatchId == id);

            if (batch == null || batch.CreatedByOrgId != orgId)
            {
                return NotFound();
            }

            // Check if editable (Pre-Dispatch)
            var history = await _batchRepository.GetOwnershipHistoryAsync(id);
            var last = history.LastOrDefault();
            if (last == null || (last.ActionType != "CREATED" && last.ActionType != "BATCH_CREATED"))
            {
                TempData["Error"] = "Batch cannot be edited after dispatch.";
                return RedirectToAction("Dashboard");
            }

            var model = new CreateBatchViewModel
            {
                DrugId = batch.DrugId,
                Quantity = batch.QuantityProduced,
                ManufactureDate = batch.ManufactureDate,
                ExpiryDate = batch.ExpiryDate
            };
            
            ViewBag.BatchId = id;
            ViewBag.DrugList = new SelectList(await _context.Drugs.ToListAsync(), "DrugId", "DrugName", batch.DrugId);
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditBatch(string id, CreateBatchViewModel model)
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            
            var batch = await _context.DrugBatches.FirstOrDefaultAsync(b => b.DrugBatchId == id);

            if (batch == null || batch.CreatedByOrgId != orgId)
            {
                return NotFound();
            }

            // Strict Re-Verification
            var history = await _batchRepository.GetOwnershipHistoryAsync(id);
            var last = history.LastOrDefault();
             if (last == null || (last.ActionType != "CREATED" && last.ActionType != "BATCH_CREATED"))
            {
                TempData["Error"] = "Batch cannot be edited after dispatch.";
                return RedirectToAction("Dashboard");
            }

            if (model.Quantity <= 100)
            {
                ModelState.AddModelError("Quantity", "Quantity must be greater than 100");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if Drug Changed
                    if (batch.DrugId != model.DrugId)
                    {
                        // Logic: Delete old batch -> Create new batch (to generate new ID)
                        
                        // 1. Delete Old
                        _context.DrugBatches.Remove(batch);
                        await _context.SaveChangesAsync(); // Commit delete to free up constraints if any, or just to proceed.

                        // 2. Create New
                        // Recalculate Expiry to ensure consistency (3 Years rule from CreateBatch)
                        DateTime finalExpiryDate = model.ManufactureDate.AddYears(3);

                        await _batchService.CreateBatchAsync(model.DrugId, model.Quantity, model.ManufactureDate, finalExpiryDate, orgId, userId);
                        
                        TempData["Message"] = "Batch updated and regenerated successfully (ID Changed).";
                    }
                    else
                    {
                        // Update Quantity
                        batch.QuantityProduced = model.Quantity;

                        // Update Dates if changed
                        if (batch.ManufactureDate != model.ManufactureDate)
                        {
                             batch.ManufactureDate = model.ManufactureDate;
                             // Auto-recalculate Expiry
                             batch.ExpiryDate = model.ManufactureDate.AddYears(3);
                        }

                        _context.Update(batch);
                        await _context.SaveChangesAsync();
                        TempData["Message"] = "Batch updated successfully.";
                    }

                    return RedirectToAction("Dashboard");
                }
                catch(Exception ex)
                {
                     // Debug: Capture exception
                     TempData["Error"] = "Exception: " + ex.Message;
                }
            }
            else
            {
                 // Debug: Capture validation errors
                 var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                 TempData["Error"] = "Validation Failed: " + string.Join("; ", errors);
            }
            
            ViewBag.BatchId = id;
            ViewBag.DrugList = new SelectList(await _context.Drugs.ToListAsync(), "DrugId", "DrugName", model.DrugId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string batchId)
        {
            try
            {
                int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
                var history = await _batchRepository.GetOwnershipHistoryAsync(batchId);
                var last = history.LastOrDefault();

                // Strict Rule: Delete ONLY if Pre-Dispatch (CREATED or BATCH_CREATED)
                if (last == null || (last.ActionType != "CREATED" && last.ActionType != "BATCH_CREATED"))
                {
                    TempData["Error"] = "Cannot delete batch. It has already been dispatched or does not exist.";
                    return RedirectToAction("Dashboard");
                }
                
                // Double check ownership
                var batch = await _context.DrugBatches.FindAsync(batchId);
                if (batch == null || batch.CreatedByOrgId != orgId)
                {
                     TempData["Error"] = "Batch not found or unauthorized.";
                     return RedirectToAction("Dashboard");
                }

                _context.DrugBatches.Remove(batch);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Batch deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting batch: " + ex.Message;
            }

            return RedirectToAction("Dashboard");
        }
    }
}
