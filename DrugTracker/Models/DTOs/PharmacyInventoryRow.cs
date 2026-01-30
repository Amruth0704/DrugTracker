using System;

namespace DrugTracker.Models.DTOs
{
    public class PharmacyInventoryRow
    {
        public string DrugBatchId { get; set; } = string.Empty;
        public string DrugName { get; set; } = string.Empty;
        public int AvailableQty { get; set; }
        public int ReceivedQty { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
