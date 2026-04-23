using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class Sport
    {
        [Key]
        [Column("SportId")]
        public int Id { get; set; }

        [Column("SportName")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Icon { get; set; }

        [MaxLength(500)]
        public string? URL { get; set; }

        public int PopularityScore { get; set; }

        // Navigation Properties
        public virtual ICollection<CoachSport> CoachSports { get; set; } = new List<CoachSport>();
        public virtual ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();
        public virtual ICollection<SportProduct> SportProducts { get; set; } = new List<SportProduct>();
        public virtual ICollection<RecommendedSport> RecommendedSports { get; set; } = new List<RecommendedSport>();
    }
}