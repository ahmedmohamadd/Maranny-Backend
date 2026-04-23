using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Sessions
{
    public class CreateBookingDto
    {
        [Required(ErrorMessage = "Session ID is required")]
        public int SessionID { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}