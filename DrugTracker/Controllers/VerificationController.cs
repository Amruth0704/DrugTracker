using DrugTracker.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DrugTracker.Controllers
{
    public class VerificationController : Controller
    {
        private readonly IDrugBatchRepository _batchRepository;
        private readonly IBlockchainLedgerRepository _ledgerRepository;

        public VerificationController(IDrugBatchRepository batchRepository, IBlockchainLedgerRepository ledgerRepository)
        {
            _batchRepository = batchRepository;
            _ledgerRepository = ledgerRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Track(string batchId)
        {
            if (string.IsNullOrEmpty(batchId)) return RedirectToAction("Index");

            var batch = await _batchRepository.GetByBatchIdAsync(batchId);
            if (batch == null)
            {
                ViewBag.Error = "Batch not found.";
                return View("Index");
            }

            var history = await _batchRepository.GetOwnershipHistoryAsync(batchId);
            var ledger = await _ledgerRepository.GetLedgerByBatchIdAsync(batchId); // For validation visualization

            ViewBag.History = history;
            ViewBag.Ledger = ledger;
            
            return View("Details", batch);
        }
    }
}
