using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class UserPreferences
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        // Sports preferences - stored as JSON string
        [Column(TypeName = "nvarchar(max)")]
        public string? Sports { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? BudgetMin { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? BudgetMax { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxDistance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}