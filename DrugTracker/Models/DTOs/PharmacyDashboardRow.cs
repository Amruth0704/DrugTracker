using System;

namespace DrugTracker.Models.DTOs
{
    public class PharmacyDashboardRow
    {
        public string DrugBatchId { get; set; } = string.Empty;
        public string DrugName { get; set; } = string.Empty;
        public int QuantityProduced { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LatestAction { get; set; }
    }
}
