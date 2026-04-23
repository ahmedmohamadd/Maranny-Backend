using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Maranny.Core.Enums;

namespace Maranny.Core.Entities
{
    public class ApplicationUser : IdentityUser<int>
    {
        // User Type & Status
        public UserType PrimaryUserType { get; set; } = UserType.Client;
        public bool IsBlocked { get; set; } = false;
        public string? BlockReason { get; set; }
        public int? BlockedByAdminId { get; set; }
        public DateTime? BlockedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (one-to-one relationships)
        public virtual Admin? Admin { get; set; }
        public virtual Coach? Coach { get; set; }
        public virtual Client? Client { get; set; }
        public virtual UserPreferences? UserPreferences { get; set; }

        // Navigation Properties (one-to-many)
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<ClientNotification> ClientNotifications { get; set; } = new List<ClientNotification>();
        public virtual ICollection<CoachNotification> CoachNotifications { get; set; } = new List<CoachNotification>();
    }
}