using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Bookings
{
    public class CoachBookingActionDto
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
