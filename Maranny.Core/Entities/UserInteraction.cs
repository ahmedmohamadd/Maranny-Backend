using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class UserInteraction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CoachId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // "View", "Click", "Booking"

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Optional: Store additional context
        [MaxLength(500)]
        public string? Context { get; set; } // e.g., "Viewed from search results"

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(CoachId))]
        public virtual Coach Coach { get; set; } = null!;
    }
}