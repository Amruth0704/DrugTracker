using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrugTracker.Models
{
    public class BatchOwnershipHistory
    {
        [Key]
        public int OwnershipId { get; set; }

        [Required]
        [StringLength(100)]
        public string DrugBatchId { get; set; } = string.Empty;

        [ForeignKey("DrugBatchId")]
        public DrugBatch? DrugBatch { get; set; }

        public int? FromOrgId { get; set; }

        [ForeignKey("FromOrgId")]
        public Organization? FromOrg { get; set; }

        [Required]
        public int ToOrgId { get; set; }

        [ForeignKey("ToOrgId")]
        public Organization? ToOrg { get; set; }

        [Required]
        [AllowedValues("CREATED", "TRANSFERRED", "RECEIVED")]
        [StringLength(30)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        public int PerformedBy { get; set; }

        [ForeignKey("PerformedBy")]
        public User? User { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}
