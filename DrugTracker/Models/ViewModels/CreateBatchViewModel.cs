using System;
using System.ComponentModel.DataAnnotations;

namespace DrugTracker.Models.ViewModels
{
    public class CreateBatchViewModel
    {
        [Required]
        [Display(Name = "Drug")]
        public int DrugId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Manufacture Date")]
        public DateTime ManufactureDate { get; set; } = DateTime.Now;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(1);
    }
}
