using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class CoachNotification
    {
        [Key, Column(Order = 0)]
        public int CoachID { get; set; }

        [Key, Column(Order = 1)]
        public int NotificationID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CoachID))]
        public virtual ApplicationUser Coach { get; set; } = null!;

        [ForeignKey(nameof(NotificationID))]
        public virtual Notification Notification { get; set; } = null!;
    }
}