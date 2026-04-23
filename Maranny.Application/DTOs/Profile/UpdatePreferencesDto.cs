using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Profile
{
    public class UpdatePreferencesDto
    {
        [MaxLength(1000)]
        public string? Sports { get; set; } // JSON array of sport names or IDs

        [Range(0, 1000000)]
        public decimal? BudgetMin { get; set; }

        [Range(0, 1000000)]
        public decimal? BudgetMax { get; set; }

        [Range(0, 1000)]
        public decimal? MaxDistance { get; set; } // in kilometers
    }
}