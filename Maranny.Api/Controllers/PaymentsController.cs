using Maranny.Application.DTOs.Payments;
using Maranny.Core.Enums;
using Maranny.Core.Interfaces;
using Maranny.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Maranny.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;

        public PaymentsController(
            IPaymentService paymentService,
            ApplicationDbContext dbContext,
            INotificationService notificationService)
        {
            _paymentService = paymentService;
            _dbContext = dbContext;
            _notificationService = notificationService;
        }

        // Initiate payment
        [HttpPost("initiate")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> InitiatePayment(InitiatePaymentDto dto)
        {
            // Get current user (client)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            // Get client ID
            var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return NotFound(new { error = "Client profile not found" });
            }

            // Verify booking exists and belongs to client
            var booking = await _dbContext.Bookings
                .Include(b => b.TrainingSession)
                    .ThenInclude(s => s.Coach)
                .FirstOrDefaultAsync(b => b.BookingID == dto.BookingID);

            if (booking == null)
            {
                return NotFound(new { error = "Booking not found" });
            }

            if (booking.ClientID != client.ClientID)
            {
                return Forbid();
            }

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            {
                return BadRequest(new { error = "Payment cannot be initiated for this booking" });
            }

            var normalizedMethod = dto.Method?.Trim();
            if (!string.Equals(normalizedMethod, "Card", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedMethod, "Wallet", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Only Card and Wallet payment methods are supported" });
            }

            var expectedAmount = await _dbContext.CoachSports
                .Where(cs => cs.CoachID == booking.TrainingSession.CoachID &&
                             cs.SportID == booking.TrainingSession.SportID)
                .Select(cs => cs.PricePerSession)
                .FirstOrDefaultAsync();

            if (!expectedAmount.HasValue || expectedAmount.Value <= 0)
            {
                return BadRequest(new { error = "Session price is not configured for this coach and sport" });
            }

            if (dto.Amount != expectedAmount.Value)
            {
                return BadRequest(new
                {
                    error = "Payment amount does not match the configured session price",
                    expectedAmount = expectedAmount.Value
                });
            }

            // Check if payment already exists
            var existingPayment = await _paymentService.GetPaymentByBookingIdAsync(dto.BookingID);
            if (existingPayment != null)
            {
                if (existingPayment.Status == PaymentStatus.Completed)
                    return BadRequest(new { error = "Payment already completed for this booking" });

                if (existingPayment.Status == PaymentStatus.Pending)
                    return BadRequest(new { error = "Payment already initiated for this booking. Please complete the existing payment." });
            }

            try
            {
                // Create payment
                var payment = await _paymentService.InitiatePaymentAsync(
                    dto.BookingID,
                    expectedAmount.Value,
                    NormalizePaymentMethod(normalizedMethod!),
                    client.ClientID
                );

                // Generate payment URL
                var paymentUrl = await _paymentService.GeneratePaymentUrlAsync(payment);

                return Ok(new
                {
                    message = "Payment initiated successfully",
                    paymentId = payment.PaymentID,
                    paymentUrl = paymentUrl,
                    amount = payment.Amount,
                    platformFee = payment.PlatformFee,
                    bookingStatus = booking.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to initiate payment", details = ex.Message });
            }
        }

        // Get payment details
        [HttpGet("{paymentId:int}")]
        public async Task<IActionResult> GetPaymentDetails(int paymentId)
        {
            // Get current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var payment = await _dbContext.Payments
                .Include(p => p.Booking)
                .Include(p => p.TrainingSession)
                    .ThenInclude(s => s.Coach)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            // Verify ownership (client or coach)
            var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            var coach = await _dbContext.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);

            bool isOwner = (client != null && payment.ClientID == client.ClientID) ||
                          (coach != null && payment.TrainingSession.CoachID == coach.CoachID);

            if (!isOwner && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var result = new
            {
                payment.PaymentID,
                payment.BookingID,
                payment.Amount,
                payment.Method,
                Status = payment.Status.ToString(),
                payment.TransactionDate,
                payment.PlatformFee,
                payment.RefundAmount,
                Session = new
                {
                    payment.TrainingSession.SessionDate,
                    payment.TrainingSession.Start_Time,
                    payment.TrainingSession.End_Time,
                    payment.TrainingSession.Location
                },
                Coach = new
                {
                    Name = payment.TrainingSession.Coach.F_name + " " + payment.TrainingSession.Coach.L_name
                }
            };

            return Ok(result);
        }

        // Webhook for payment confirmation (from Paymob)
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentWebhook([FromBody] object webhookData)
        {
            // TODO: Verify webhook signature
            // TODO: Parse Paymob webhook data
            // For now, this is a placeholder

            try
            {
                // In production, you would:
                // 1. Verify webhook signature
                // 2. Extract payment details
                // 3. Update payment status
                // 4. Send notifications

                return Ok(new { message = "Webhook received" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Get my payments (as client)
        [HttpGet("my")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetMyPayments()
        {
            // Get current user (client)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return NotFound(new { error = "Client profile not found" });
            }

            var payments = await _dbContext.Payments
                .Include(p => p.TrainingSession)
                    .ThenInclude(s => s.Coach)
                .Where(p => p.ClientID == client.ClientID)
                .OrderByDescending(p => p.TransactionDate)
                .Select(p => new
                {
                    p.PaymentID,
                    p.Amount,
                    p.Method,
                    Status = p.Status.ToString(),
                    p.TransactionDate,
                    Session = new
                    {
                        p.TrainingSession.SessionDate,
                        p.TrainingSession.Start_Time,
                        CoachName = p.TrainingSession.Coach.F_name + " " + p.TrainingSession.Coach.L_name
                    }
                })
                .ToListAsync();

            return Ok(payments);
        }

        private static string NormalizePaymentMethod(string method)
        {
            return string.Equals(method, "wallet", StringComparison.OrdinalIgnoreCase)
                ? "Wallet"
                : "Card";
        }
    }
}
