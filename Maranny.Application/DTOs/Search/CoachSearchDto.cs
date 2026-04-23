using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Search
{
    public class CoachSearchDto
    {
        // Search by name
        [MaxLength(100)]
        public string? Name { get; set; }

        // Filter by sport
        public int? SportID { get; set; }

        // Filter by location
        [MaxLength(100)]
        public string? City { get; set; }

        // Filter by minimum rating
        [Range(0, 5)]
        public decimal? MinRating { get; set; }

        // Filter by experience years
        [Range(0, 50)]
        public int? MinExperience { get; set; }

        // Filter by gender
        [MaxLength(10)]
        public string? Gender { get; set; }

        // Filter by verification status
        public bool? VerifiedOnly { get; set; } = true;

        // Pagination
        [Range(1, 1000)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        // Sorting
        [MaxLength(50)]
        public string? SortBy { get; set; } // "rating", "experience", "name"

        [MaxLength(10)]
        public string? SortOrder { get; set; } = "desc"; // "asc" or "desc"
    }
}