using DrugTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DrugTracker.Controllers
{
    // Publicly accessible controller for verification
    public class VerificationController : Controller
    {
        private readonly DrugTracker.Repositories.Interfaces.IDrugBatchRepository _batchRepository;
        private readonly DrugTracker.Repositories.Interfaces.IBlockchainLedgerRepository _ledgerRepository;

        public VerificationController(DrugTracker.Repositories.Interfaces.IDrugBatchRepository batchRepository,
                                      DrugTracker.Repositories.Interfaces.IBlockchainLedgerRepository ledgerRepository)
        {
            _batchRepository = batchRepository;
            _ledgerRepository = ledgerRepository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Verify(string batchId)
        {
            if (string.IsNullOrEmpty(batchId))
            {
                ViewBag.Error = "Please provide a valid Batch ID or Code.";
                return View("Index");
            }

            var batch = await _batchRepository.GetByBatchIdAsync(batchId);

            if (batch == null)
            {
                ViewBag.Error = "Batch not found. Please check the Batch ID / Code.";
                return View("Index"); 
            }

            // Fetch History and Ledger for Details view
            ViewBag.History = await _batchRepository.GetOwnershipHistoryAsync(batchId);
            ViewBag.Ledger = await _ledgerRepository.GetLedgerByBatchIdAsync(batchId);

            return View("Details", batch);
        }
    }
}
