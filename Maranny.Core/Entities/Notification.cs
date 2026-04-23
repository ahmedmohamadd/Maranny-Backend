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
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Message { get; set; }

        public NotificationType Type { get; set; } = NotificationType.General;

        public int? ReviewID { get; set; }

        public int? PaymentID { get; set; }

        public int? BookingID { get; set; }

        public int? AdminID { get; set; }

        public int? ProductID { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(ReviewID))]
        public virtual Review? Review { get; set; }

        [ForeignKey(nameof(PaymentID))]
        public virtual Payment? Payment { get; set; }

        [ForeignKey(nameof(BookingID))]
        public virtual Booking? Booking { get; set; }

        [ForeignKey(nameof(AdminID))]
        public virtual Admin? Admin { get; set; }

        [ForeignKey(nameof(ProductID))]
        public virtual Product? Product { get; set; }

        public virtual ICollection<ClientNotification> ClientNotifications { get; set; } = new List<ClientNotification>();
        public virtual ICollection<CoachNotification> CoachNotifications { get; set; } = new List<CoachNotification>();
        public virtual ICollection<ReportNotification> ReportNotifications { get; set; } = new List<ReportNotification>();
    }
}