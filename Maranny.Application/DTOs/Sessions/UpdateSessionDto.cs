using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Sessions
{
    public class UpdateSessionDto
    {
        public DateTime? SessionDate { get; set; }

        [MaxLength(50)]
        public string? SessionType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [Range(1, 100)]
        public int? MaxParticipants { get; set; }

        public TimeSpan? Start_Time { get; set; }

        public TimeSpan? End_Time { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; } // "Scheduled", "Completed", "Cancelled"
    }
}