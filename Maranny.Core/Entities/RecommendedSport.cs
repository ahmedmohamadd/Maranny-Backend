using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class RecommendedSport
    {
        [Key, Column(Order = 0)]
        public int RecommendationID { get; set; }

        [Key, Column(Order = 1)]
        public int SportID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(RecommendationID))]
        public virtual Recommendation Recommendation { get; set; } = null!;

        [ForeignKey(nameof(SportID))]
        public virtual Sport Sport { get; set; } = null!;
    }
}