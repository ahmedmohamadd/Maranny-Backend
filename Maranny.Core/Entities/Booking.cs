using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Maranny.Core.Enums;

namespace Maranny.Core.Entities
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required]
        public int SessionID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        
        // Cancellation tracking
        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public bool CancelledByCoach { get; set; } = false;

        // Navigation Properties
        [ForeignKey(nameof(SessionID))]
        public virtual TrainingSession TrainingSession { get; set; } = null!;

        [ForeignKey(nameof(ClientID))]
        public virtual Client Client { get; set; } = null!;

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}