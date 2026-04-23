using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Sessions
{
    public class CreateSessionDto
    {
        [Required(ErrorMessage = "Sport ID is required")]
        public int SportID { get; set; }

        [Required(ErrorMessage = "Session date is required")]
        public DateTime SessionDate { get; set; }

        [Required(ErrorMessage = "Session type is required")]
        [MaxLength(50)]
        public string SessionType { get; set; } = string.Empty; // "Individual", "Group"

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Maximum participants is required")]
        [Range(1, 100, ErrorMessage = "Max participants must be between 1 and 100")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan Start_Time { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan End_Time { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}