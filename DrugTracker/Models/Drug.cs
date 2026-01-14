using System.ComponentModel.DataAnnotations;

namespace DrugTracker.Models
{
    public class Drug
    {
        [Key]
        public int DrugId { get; set; }

        [Required]
        [StringLength(50)]
        public string DrugCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DrugName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
