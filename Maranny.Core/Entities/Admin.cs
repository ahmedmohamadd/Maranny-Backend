using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string F_name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string L_name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Username { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public DateTime? LastLogin { get; set; }

        [MaxLength(500)]
        public string? Permissions { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<AdminPhone> AdminPhones { get; set; } = new List<AdminPhone>();
        public virtual ICollection<ClientAdmin> ClientAdmins { get; set; } = new List<ClientAdmin>();
        public virtual ICollection<AdminReport> AdminReports { get; set; } = new List<AdminReport>();
        public virtual ICollection<AdminProduct> AdminProducts { get; set; } = new List<AdminProduct>();
        public virtual ICollection<CoachAdmin> CoachAdmins { get; set; } = new List<CoachAdmin>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}