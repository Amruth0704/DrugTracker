using DrugTracker.Models;
using DrugTracker.Models.ViewModels;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net;
using System.Net.Sockets;

namespace DrugTracker.Controllers
{
    [Authorize(Roles = "Manufacturer", AuthenticationSchemes = "ManufacturerScheme")]
    public class ManufacturerController : Controller
    {
        private readonly IBatchService _batchService;
        private readonly IDrugBatchRepository _batchRepository;
        private readonly DrugTracker.Data.DrugTrackerDbContext _context; // Direct context access for dropdowns/lookups if repository doesn't have it
        private readonly IConfiguration _configuration; // Added for IConfiguration

        public ManufacturerController(IBatchService batchService, IDrugBatchRepository batchRepository, DrugTracker.Data.DrugTrackerDbContext context, IConfiguration configuration)
        {
            _batchService = batchService;
            _batchRepository = batchRepository;
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Dashboard()
        {
            int orgId = int.Parse(User.FindFirst("OrgId")?.Value ?? "0");
            
            // Fetch consolidated data using the new Stored Procedure
            var dashboardData = await _batchRepository.GetManufacturerDashboardDataAsync(orgId);
            
            // Populate Distributors for Dispatch Modal
            ViewBag.Distributors = await _context.Organizations
                .Where(o => o.OrgType == "DISTRIBUTOR")
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
                LatestAction = d.LatestAction ?? "CREATED",
                IsActionEnabled = (string.IsNullOrEmpty(d.LatestAction) || 
                                 string.Equals(d.LatestAction, "CREATED", StringComparison.OrdinalIgnoreCase) || 
                                 string.Equals(d.LatestAction, "BATCH_CREATED", StringComparison.OrdinalIgnoreCase)),
                TransferredToOrgName = d.TransferredToOrgName ?? "N/A"
            }).ToList();



            // QR Code Generation
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            {
                foreach (var vm in viewModels)
                {
                    // Generate Unique Verification Link
                    string publicBaseUrl = _configuration["PublicBaseUrl"];
                    string host = Request.Host.Value;
                    string scheme = Request.Scheme;

                    if (!string.IsNullOrEmpty(publicBaseUrl))
                    {
                        // Ensure protocol is present
                        if (!publicBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                            !publicBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            publicBaseUrl = "http://" + publicBaseUrl;
                        }

                        publicBaseUrl = publicBaseUrl.TrimEnd('/');
                        string verificationUrl = $"{publicBaseUrl}/Verification/Verify?batchId={vm.Batch.DrugBatchId}";
                        GenerateQRCode(vm, qrGenerator, verificationUrl);
                    }
                    else
                    {
                        // Fallback to auto-detection (Same WiFi / Localhost)
                        if (host.Contains("localhost") || host.Contains("127.0.0.1"))
                        {
                            var localIp = GetLocalIPAddress();
                            if (!string.IsNullOrEmpty(localIp))
                            {
                                host = host.Replace("localhost", localIp).Replace("127.0.0.1", localIp);
                            }
                        }
                        string verificationUrl = $"{scheme}://{host}/Verification/Verify?batchId={vm.Batch.DrugBatchId}";
                        GenerateQRCode(vm, qrGenerator, verificationUrl);
                    }
                }
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
            if (last != null && 
                !string.Equals(last.ActionType, "CREATED", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(last.ActionType, "BATCH_CREATED", StringComparison.OrdinalIgnoreCase))
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
             if (last != null && 
                 !string.Equals(last.ActionType, "CREATED", StringComparison.OrdinalIgnoreCase) && 
                 !string.Equals(last.ActionType, "BATCH_CREATED", StringComparison.OrdinalIgnoreCase))
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
                        // Use BatchService which calls the Stored Procedure
                        // The SP handles: Table Update + Blockchain Ledger entry
                        batch.DrugId = model.DrugId;
                        batch.QuantityProduced = model.Quantity;
                        batch.ManufactureDate = model.ManufactureDate;
                        batch.ExpiryDate = model.ManufactureDate.AddYears(3);

                        await _batchService.UpdateBatchAsync(batch, userId);
                        TempData["Message"] = "Batch updated successfully via Stored Procedure.";
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
                
                // BatchService.DeleteBatchAsync now calls the Stored Procedure.
                // The SP handles: Ownership Check + Blockchain Ledger + Batch Deletion
                await _batchService.DeleteBatchAsync(batchId, orgId);
                
                TempData["Message"] = "Batch deleted successfully.";
            }
            catch(Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Dashboard");
        }

        private void GenerateQRCode(BatchViewModel vm, QRCoder.QRCodeGenerator qrGenerator, string verificationUrl)
        {
            var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                string base64Qr = Convert.ToBase64String(qrCodeBytes);
                vm.QRCodeImage = $"data:image/png;base64,{base64Qr}";
            }
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }
    }
}
