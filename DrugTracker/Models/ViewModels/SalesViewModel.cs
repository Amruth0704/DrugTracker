using System.ComponentModel.DataAnnotations;

namespace DrugTracker.Models.ViewModels
{
    public class SalesViewModel
    {
        [Required]
        public string DrugBatchId { get; set; } = string.Empty;

        public string DrugName { get; set; } = string.Empty; // Display only
        
        public int AvailableQty { get; set; } // Display only
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int QuantityToSell { get; set; }
    }
}
