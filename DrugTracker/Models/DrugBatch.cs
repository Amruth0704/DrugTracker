using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrugTracker.Models
{
    public class DrugBatch : IValidatableObject
    {
        [Key]
        [StringLength(100)]
        public string DrugBatchId { get; set; } = string.Empty;

        [Required]
        public int DrugId { get; set; }

        [ForeignKey("DrugId")]
        public Drug? Drug { get; set; }

        [Required]
        [Range(101, int.MaxValue)]
        public int QuantityProduced { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ManufactureDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public int CreatedByOrgId { get; set; }

        [ForeignKey("CreatedByOrgId")]
        public Organization? CreatedByOrg { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ManufactureDate > DateTime.Now)
            {
                yield return new ValidationResult("Manufacture Date cannot be in the future.", new[] { nameof(ManufactureDate) });
            }
            if (ExpiryDate <= ManufactureDate)
            {
                yield return new ValidationResult("ExpiryDate must be greater than ManufactureDate.", new[] { nameof(ExpiryDate), nameof(ManufactureDate) });
            }
        }
    }
}
