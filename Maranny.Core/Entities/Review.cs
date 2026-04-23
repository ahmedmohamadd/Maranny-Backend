using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int SessionID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public int CoachID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        [MaxLength(1000)]
        public string? CoachResponse { get; set; }

        public DateTime? ResponseDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(SessionID))]
        public virtual TrainingSession TrainingSession { get; set; } = null!;

        [ForeignKey(nameof(ClientID))]
        public virtual Client Client { get; set; } = null!;

        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}