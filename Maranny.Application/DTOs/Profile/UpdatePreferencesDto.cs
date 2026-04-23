using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Profile
{
    public class UpdatePreferencesDto
    {
        public List<string>? Sports { get; set; }

        [Range(0, 1000000)]
        public decimal? BudgetMin { get; set; }

        [Range(0, 1000000)]
        public decimal? BudgetMax { get; set; }

        [Range(0, 1000)]
        public decimal? MaxDistance { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Area { get; set; }

        [MaxLength(50)]
        public string? LocationPreference { get; set; }

        [MaxLength(20)]
        public string? RatingPreference { get; set; }

        [MaxLength(30)]
        public string? CoachGender { get; set; }

        [MaxLength(30)]
        public string? CoachAgeRange { get; set; }

        public bool? CertifiedOnly { get; set; }
    }
}
