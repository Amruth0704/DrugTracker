using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrugTracker.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityId { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [StringLength(100)]
        public string? Action { get; set; }

        [StringLength(50)]
        public string? EntityType { get; set; }

        [StringLength(100)]
        public string? EntityId { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}
