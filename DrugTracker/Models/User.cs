using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrugTracker.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(150)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        [AllowedValues("ADMIN", "MANUFACTURER", "DISTRIBUTOR", "PHARMACY")]
        [StringLength(30)]
        public string Role { get; set; } = string.Empty;

        [Required]
        public int OrgId { get; set; }

        [ForeignKey("OrgId")]
        public Organization? Organization { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
