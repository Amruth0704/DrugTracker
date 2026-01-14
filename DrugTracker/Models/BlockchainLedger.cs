using System.ComponentModel.DataAnnotations;

namespace DrugTracker.Models
{
    public class BlockchainLedger
    {
        [Key]
        public int LedgerId { get; set; }

        [Required]
        [StringLength(100)]
        public string DrugBatchId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        public int? FromOrgId { get; set; }
        public int? ToOrgId { get; set; }
        public int? Quantity { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}
