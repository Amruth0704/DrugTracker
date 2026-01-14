using System.ComponentModel.DataAnnotations;

namespace DrugTracker.Models
{
    public class Organization
    {
        [Key]
        public int OrgId { get; set; }

        [Required]
        [StringLength(200)]
        public string OrgName { get; set; } = string.Empty;

        [Required]
        [AllowedValues("MANUFACTURER", "DISTRIBUTOR", "PHARMACY", "REGULATOR")]
        [StringLength(30)]
        public string OrgType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
