using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class Recommendation
    {
        [Key]
        public int RecommendationID { get; set; }

        [Required]
        public int CoachID { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;

        public virtual ICollection<ClientRecommendation> ClientRecommendations { get; set; } = new List<ClientRecommendation>();
        public virtual ICollection<RecommendedSport> RecommendedSports { get; set; } = new List<RecommendedSport>();
    }
}