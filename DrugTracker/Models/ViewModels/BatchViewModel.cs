using DrugTracker.Models;

namespace DrugTracker.Models.ViewModels
{
    public class BatchViewModel
    {
        public DrugBatch Batch { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string LatestAction { get; set; } = string.Empty;
        public bool IsActionEnabled { get; set; } = true;
        public string TransferredToOrgName { get; set; } = "N/A";
    }
}
