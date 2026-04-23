using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class ReportNotification
    {
        [Key, Column(Order = 0)]
        public int ReportID { get; set; }

        [Key, Column(Order = 1)]
        public int NotificationID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ReportID))]
        public virtual Report Report { get; set; } = null!;

        [ForeignKey(nameof(NotificationID))]
        public virtual Notification Notification { get; set; } = null!;
    }
}