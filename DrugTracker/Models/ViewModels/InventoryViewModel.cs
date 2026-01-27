using System;

namespace DrugTracker.Models.ViewModels
{
    public class InventoryViewModel
    {
        public string DrugBatchId { get; set; } = string.Empty;
        public string DrugName { get; set; } = string.Empty;
        public int ReceivedQty { get; set; }
        public int QuantitySold { get; set; }
        public int AvailableQty { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
