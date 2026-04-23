using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class CoachLocation
    {
        [Key, Column(Order = 0)]
        public int CoachID { get; set; }

        [Key, Column(Order = 1)]
        [MaxLength(500)]
        public string WorkingLocation { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;
    }
}