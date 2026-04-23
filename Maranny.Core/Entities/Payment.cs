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
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int SessionID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? Method { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PlatformFee { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(200)]
        public string? PaymentGateway { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? RefundAmount { get; set; }
        public DateTime? RefundedAt { get; set; }
        [MaxLength(500)]
        public string? RefundReason { get; set; }
        public bool IsRefunded { get; set; } = false;

        // Navigation Properties
        [ForeignKey(nameof(SessionID))]
        public virtual TrainingSession TrainingSession { get; set; } = null!;

        [ForeignKey(nameof(BookingID))]
        public virtual Booking Booking { get; set; } = null!;

        [ForeignKey(nameof(ClientID))]
        public virtual Client Client { get; set; } = null!;

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}