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
    public class TrainingSession
    {
        [Key]
        public int SessionID { get; set; }

        [Required]
        public int CoachID { get; set; }

        [Required]
        public int SportID { get; set; }

        [Required]
        public DateTime SessionDate { get; set; }

        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        [MaxLength(100)]
        public string? SessionType { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        public int? MaxParticipants { get; set; }

        [Required]
        public TimeSpan Start_Time { get; set; }

        [Required]
        public TimeSpan End_Time { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;

        [ForeignKey(nameof(SportID))]
        public virtual Sport Sport { get; set; } = null!;

        public virtual ICollection<ClientSession> ClientSessions { get; set; } = new List<ClientSession>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual Booking? Booking { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}