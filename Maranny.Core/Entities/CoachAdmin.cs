using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class CoachAdmin
    {
        [Key, Column(Order = 0)]
        public int AdminID { get; set; }

        [Key, Column(Order = 1)]
        public int CoachID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AdminID))]
        public virtual Admin Admin { get; set; } = null!;

        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;
    }
}