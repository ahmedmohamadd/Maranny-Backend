using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class CoachClient
    {
        [Key, Column(Order = 0)]
        public int ClientID { get; set; }

        [Key, Column(Order = 1)]
        public int CoachID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ClientID))]
        public virtual Client Client { get; set; } = null!;

        [ForeignKey(nameof(CoachID))]
        public virtual Coach Coach { get; set; } = null!;
    }
}