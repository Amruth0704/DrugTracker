using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrugTracker.Models
{
    public class Inventory
    {
        [Key]
        [StringLength(100)]
        public string DrugBatchId { get; set; } = string.Empty;

        [ForeignKey("DrugBatchId")]
        public DrugBatch? DrugBatch { get; set; }

        [Required]
        public int PharmacyOrgId { get; set; }

        [ForeignKey("PharmacyOrgId")]
        public Organization? PharmacyOrg { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int AvailableQty { get; set; }

        public int ReceivedQty { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
