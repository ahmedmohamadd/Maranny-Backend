using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Maranny.Core.Entities;

namespace Maranny.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Coach> Coaches { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Sport> Sports { get; set; }
        public DbSet<CoachSport> CoachSports { get; set; }
        public DbSet<TrainingSession> TrainingSessions { get; set; }
        public DbSet<ClientSession> ClientSessions { get; set; }
        public DbSet<CoachClient> CoachClients { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SportProduct> SportProducts { get; set; }
        public DbSet<Recommendation> Recommendations { get; set; }
        public DbSet<ClientRecommendation> ClientRecommendations { get; set; }
        public DbSet<RecommendedSport> RecommendedSports { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ClientReport> ClientReports { get; set; }
        public DbSet<AdminReport> AdminReports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ClientNotification> ClientNotifications { get; set; }
        public DbSet<CoachNotification> CoachNotifications { get; set; }
        public DbSet<ReportNotification> ReportNotifications { get; set; }
        public DbSet<AdminPhone> AdminPhones { get; set; }
        public DbSet<ClientPhone> ClientPhones { get; set; }
        public DbSet<CoachLocation> CoachLocations { get; set; }
        public DbSet<ClientAdmin> ClientAdmins { get; set; }
        public DbSet<CoachAdmin> CoachAdmins { get; set; }
        public DbSet<AdminProduct> AdminProducts { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserInteraction> UserInteractions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite keys for junction tables
            modelBuilder.Entity<ClientSession>()
                .HasKey(cs => new { cs.ClientID, cs.SessionID });

            modelBuilder.Entity<CoachClient>()
                .HasKey(cc => new { cc.ClientID, cc.CoachID });

            modelBuilder.Entity<SportProduct>()
                .HasKey(sp => new { sp.SportID, sp.ProductID });

            modelBuilder.Entity<ClientRecommendation>()
                .HasKey(cr => new { cr.ClientID, cr.RecommendationID });

            modelBuilder.Entity<RecommendedSport>()
                .HasKey(rs => new { rs.RecommendationID, rs.SportID });

            modelBuilder.Entity<ClientReport>()
                .HasKey(cr => new { cr.ClientID, cr.ReportID });

            modelBuilder.Entity<AdminReport>()
                .HasKey(ar => new { ar.ReportID, ar.AdminID });

            modelBuilder.Entity<ClientNotification>()
                .HasKey(cn => new { cn.ClientID, cn.NotificationID });

            modelBuilder.Entity<CoachNotification>()
                .HasKey(cn => new { cn.CoachID, cn.NotificationID });

            modelBuilder.Entity<ReportNotification>()
                .HasKey(rn => new { rn.ReportID, rn.NotificationID });

            modelBuilder.Entity<AdminPhone>()
                .HasKey(ap => new { ap.AdminID, ap.Phone });

            modelBuilder.Entity<ClientPhone>()
                .HasKey(cp => new { cp.ClientID, cp.Phone });

            modelBuilder.Entity<CoachLocation>()
                .HasKey(cl => new { cl.CoachID, cl.WorkingLocation });

            modelBuilder.Entity<ClientAdmin>()
                .HasKey(ca => new { ca.ClientID, ca.AdminID });

            modelBuilder.Entity<CoachAdmin>()
                .HasKey(ca => new { ca.AdminID, ca.CoachID });

            modelBuilder.Entity<AdminProduct>()
                .HasKey(ap => new { ap.AdminID, ap.ProductID });

            // Configure one-to-one relationships between ApplicationUser and Admin/Coach/Client
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Admin)
                .WithOne(a => a.User)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Coach)
                .WithOne(c => c.User)
                .HasForeignKey<Coach>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Client)
                .WithOne(c => c.User)
                .HasForeignKey<Client>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-one relationship between ApplicationUser and UserPreferences
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.UserPreferences)
                .WithOne(up => up.User)
                .HasForeignKey<UserPreferences>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship between ApplicationUser and RefreshToken
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship between ApplicationUser and PasswordResetToken
            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(prt => prt.User)
                .WithMany()
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique index on Sport Name
            modelBuilder.Entity<Sport>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // Configure CoachSport unique constraint
            modelBuilder.Entity<CoachSport>()
                .HasIndex(cs => new { cs.CoachID, cs.SportID })
                .IsUnique();

            // Configure Category self-referencing relationship
            modelBuilder.Entity<Category>()
                .HasOne(c => c.MainCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.MainCategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade delete conflicts
            modelBuilder.Entity<TrainingSession>()
                .HasOne(s => s.Coach)
                .WithMany(c => c.TrainingSessions)
                .HasForeignKey(s => s.CoachID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Client)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Coach)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CoachID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Client)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientAdmin>()
                .HasOne(ca => ca.Client)
                .WithMany(c => c.ClientAdmins)
                .HasForeignKey(ca => ca.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientAdmin>()
                .HasOne(ca => ca.Admin)
                .WithMany(a => a.ClientAdmins)
                .HasForeignKey(ca => ca.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoachAdmin>()
                .HasOne(ca => ca.Coach)
                .WithMany(c => c.CoachAdmins)
                .HasForeignKey(ca => ca.CoachID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoachAdmin>()
                .HasOne(ca => ca.Admin)
                .WithMany(a => a.CoachAdmins)
                .HasForeignKey(ca => ca.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoachClient>()
                .HasOne(cc => cc.Coach)
                .WithMany(c => c.CoachClients)
                .HasForeignKey(cc => cc.CoachID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoachClient>()
                .HasOne(cc => cc.Client)
                .WithMany(c => c.CoachClients)
                .HasForeignKey(cc => cc.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminProduct>()
                .HasOne(ap => ap.Admin)
                .WithMany(a => a.AdminProducts)
                .HasForeignKey(ap => ap.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminProduct>()
                .HasOne(ap => ap.Product)
                .WithMany(p => p.AdminProducts)
                .HasForeignKey(ap => ap.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminReport>()
                .HasOne(ar => ar.Admin)
                .WithMany(a => a.AdminReports)
                .HasForeignKey(ar => ar.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminReport>()
                .HasOne(ar => ar.Report)
                .WithMany(r => r.AdminReports)
                .HasForeignKey(ar => ar.ReportID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientReport>()
                .HasOne(cr => cr.Client)
                .WithMany(c => c.ClientReports)
                .HasForeignKey(cr => cr.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientReport>()
                .HasOne(cr => cr.Report)
                .WithMany(r => r.ClientReports)
                .HasForeignKey(cr => cr.ReportID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientSession>()
                .HasOne(cs => cs.Client)
                .WithMany(c => c.ClientSessions)
                .HasForeignKey(cs => cs.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientSession>()
                .HasOne(cs => cs.TrainingSession)
                .WithMany(s => s.ClientSessions)
                .HasForeignKey(cs => cs.SessionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientNotification>()
                .HasOne(cn => cn.Client)
                .WithMany(c => c.ClientNotifications)
                .HasForeignKey(cn => cn.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoachNotification>()
                .HasOne(cn => cn.Coach)
                .WithMany(c => c.CoachNotifications)
                .HasForeignKey(cn => cn.CoachID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientRecommendation>()
                .HasOne(cr => cr.Client)
                .WithMany(c => c.ClientRecommendations)
                .HasForeignKey(cr => cr.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientRecommendation>()
                .HasOne(cr => cr.Recommendation)
                .WithMany(r => r.ClientRecommendations)
                .HasForeignKey(cr => cr.RecommendationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SportProduct>()
                .HasOne(sp => sp.Sport)
                .WithMany(s => s.SportProducts)
                .HasForeignKey(sp => sp.SportID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SportProduct>()
                .HasOne(sp => sp.Product)
                .WithMany(p => p.SportProducts)
                .HasForeignKey(sp => sp.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReportNotification>()
                .HasOne(rn => rn.Report)
                .WithMany(r => r.ReportNotifications)
                .HasForeignKey(rn => rn.ReportID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReportNotification>()
                .HasOne(rn => rn.Notification)
                .WithMany(n => n.ReportNotifications)
                .HasForeignKey(rn => rn.NotificationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Coach)
                .WithMany()
                .HasForeignKey(r => r.CoachID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix cascade delete conflicts for Notifications
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Review)
                .WithMany(r => r.Notifications)
                .HasForeignKey(n => n.ReviewID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Payment)
                .WithMany(p => p.Notifications)
                .HasForeignKey(n => n.PaymentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Booking)
                .WithMany(b => b.Notifications)
                .HasForeignKey(n => n.BookingID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Admin)
                .WithMany(a => a.Notifications)
                .HasForeignKey(n => n.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Product)
                .WithMany(p => p.Notifications)
                .HasForeignKey(n => n.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix cascade delete conflicts for RecommendedSport
            modelBuilder.Entity<RecommendedSport>()
                .HasOne(rs => rs.Recommendation)
                .WithMany(r => r.RecommendedSports)
                .HasForeignKey(rs => rs.RecommendationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecommendedSport>()
                .HasOne(rs => rs.Sport)
                .WithMany(s => s.RecommendedSports)
                .HasForeignKey(rs => rs.SportID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix cascade delete conflicts for CoachNotification
            modelBuilder.Entity<CoachNotification>()
                .HasOne(cn => cn.Notification)
                .WithMany(n => n.CoachNotifications)
                .HasForeignKey(cn => cn.NotificationID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix cascade delete conflicts for ClientNotification
            modelBuilder.Entity<ClientNotification>()
                .HasOne(cn => cn.Notification)
                .WithMany(n => n.ClientNotifications)
                .HasForeignKey(cn => cn.NotificationID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix cascade delete for Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.TrainingSession)
                .WithOne(s => s.Payment)
                .HasForeignKey<Payment>(p => p.SessionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix cascade delete for Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.TrainingSession)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.SessionID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure UserInteraction relationships
            modelBuilder.Entity<UserInteraction>()
                .HasOne(ui => ui.User)
                .WithMany()
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserInteraction>()
                .HasOne(ui => ui.Coach)
                .WithMany()
                .HasForeignKey(ui => ui.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create index on UserId and CoachId for faster queries
            modelBuilder.Entity<UserInteraction>()
                .HasIndex(ui => ui.UserId);

            modelBuilder.Entity<UserInteraction>()
                .HasIndex(ui => ui.CoachId);

            modelBuilder.Entity<UserInteraction>()
                .HasIndex(ui => ui.Timestamp);

            // Configure ChatMessage relationships
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Receiver)
                .WithMany()
                .HasForeignKey(cm => cm.ReceiverID)
                .OnDelete(DeleteBehavior.Restrict);

            // Create indexes for better query performance
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(cm => new { cm.SenderID, cm.ReceiverID });

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(cm => cm.SentAt);
        }
    }
}