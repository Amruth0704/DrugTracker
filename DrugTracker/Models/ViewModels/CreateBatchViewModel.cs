using System;
using System.ComponentModel.DataAnnotations;

namespace DrugTracker.Models.ViewModels
{
    public class CreateBatchViewModel : IValidatableObject
    {
        [Required]
        [Display(Name = "Drug")]
        public int DrugId { get; set; }

        [Required]
        [Range(101, int.MaxValue, ErrorMessage = "Quantity must be greater than 100")]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Manufacture Date")]
        public DateTime ManufactureDate { get; set; } = DateTime.Now;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(1);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ManufactureDate > DateTime.Today)
            {
                yield return new ValidationResult("Manufacture Date cannot be in the future.", new[] { nameof(ManufactureDate) });
            }
        }
    }
}
