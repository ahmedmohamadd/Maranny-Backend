using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Profile
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MinLength(2, ErrorMessage = "Last name must be at least 2 characters")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        // Client-specific fields (optional)
        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(200)]
        public string? Street { get; set; }

        [MaxLength(50)]
        public string? BuildingNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; } // "Male", "Female"

        // Coach-specific fields (optional)
        [MaxLength(1000)]
        public string? Bio { get; set; }

        public int? ExperienceYears { get; set; }

        [MaxLength(500)]
        public string? CertificateUrl { get; set; }
    }
}