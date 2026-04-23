using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Reviews
{
    public class CoachResponseDto
    {
        [Required(ErrorMessage = "Response is required")]
        [MinLength(10, ErrorMessage = "Response must be at least 10 characters")]
        [MaxLength(1000, ErrorMessage = "Response cannot exceed 1000 characters")]
        public string Response { get; set; } = string.Empty;
    }
}