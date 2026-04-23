using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class CoachSport
    {
        [Key]
        public int CoachSportID { get; set; }

        [Required]
        public int CoachID { get; set; }

        [Required]
        public int SportID { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PricePerSession { get; set; }

        public int? ExperienceYears { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;

        [ForeignKey(nameof(SportID))]
        public virtual Sport Sport { get; set; } = null!;
    }
}