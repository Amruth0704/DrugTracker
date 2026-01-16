using DrugTracker.Models;

namespace DrugTracker.Models.ViewModels
{
    public class BatchViewModel
    {
        public DrugBatch Batch { get; set; }
        public string LatestAction { get; set; } = string.Empty;
        public bool IsActionEnabled { get; set; } = true;
    }
}
