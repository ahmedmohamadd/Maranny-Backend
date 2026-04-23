using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Auth
{
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Google ID token is required")]
        public string IdToken { get; set; } = string.Empty;
                
        // Optional: User type if registering for first time
        public string? UserType { get; set; } = "Client"; // "Client" or "Coach"
    }
}