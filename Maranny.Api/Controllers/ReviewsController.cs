using Maranny.Application.DTOs.Reviews;
using Maranny.Core.Entities;
using Maranny.Core.Enums;
using Maranny.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Maranny.API.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ReviewsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SubmitReview(SubmitReviewDto dto)
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

            // Verify session exists
            var session = await _dbContext.TrainingSessions
                .FirstOrDefaultAsync(s => s.SessionID == dto.SessionID);

            if (session == null)
            {
                return NotFound(new { error = "Session not found" });
            }

            // Verify session belongs to the specified coach
            if (session.CoachID != dto.CoachID)
            {
                return BadRequest(new { error = "Session does not belong to this coach" });
            }

            // Verify client attended this session
            var clientSession = await _dbContext.ClientSessions
                .FirstOrDefaultAsync(cs => cs.ClientID == client.ClientID && cs.SessionID == dto.SessionID);

            if (clientSession == null)
            {
                return BadRequest(new { error = "You did not attend this session" });
            }

            // Verify session is completed
            if (session.Status != SessionStatus.Completed)
            {
                return BadRequest(new { error = "Cannot review a session that is not completed" });
            }

            // Check if review already exists (one review per client per coach per session)
            var existingReview = await _dbContext.Reviews
                .FirstOrDefaultAsync(r => r.ClientID == client.ClientID && r.SessionID == dto.SessionID);

            if (existingReview != null)
            {
                return BadRequest(new { error = "You have already reviewed this session" });
            }

            // Create review
            var review = new Review
            {
                SessionID = dto.SessionID,
                ClientID = client.ClientID,
                CoachID = dto.CoachID,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Reviews.Add(review);
            await _dbContext.SaveChangesAsync();

            // Update coach average rating
            await UpdateCoachAverageRating(dto.CoachID);

            return Ok(new
            {
                message = "Review submitted successfully",
                reviewId = review.ReviewID
            });
        }

        [HttpGet("coach/{coachId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCoachReviews(int coachId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Verify coach exists
            var coach = await _dbContext.Coaches.FindAsync(coachId);
            if (coach == null)
            {
                return NotFound(new { error = "Coach not found" });
            }

            // Get reviews with pagination
            var query = _dbContext.Reviews
                .Include(r => r.Client)
                .Where(r => r.CoachID == coachId)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.ReviewID,
                    r.Rating,
                    r.Comment,
                    r.CoachResponse,
                    r.ResponseDate,
                    r.CreatedAt,
                    client = new
                    {
                        name = r.Client.F_name + " " + r.Client.L_name,
                        profilePicture = r.Client.URL
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                averageRating = coach.AvgRating,
                reviews = reviews
            });
        }

        [HttpPut("{reviewId}/response")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> RespondToReview(int reviewId, CoachResponseDto dto)
        {
            // Get current user (coach)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            // Get coach ID
            var coach = await _dbContext.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);
            if (coach == null)
            {
                return NotFound(new { error = "Coach profile not found" });
            }

            // Get review
            var review = await _dbContext.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return NotFound(new { error = "Review not found" });
            }

            // Verify review is for this coach
            if (review.CoachID != coach.CoachID)
            {
                return Forbid();
            }

            // Check if coach already responded
            if (!string.IsNullOrEmpty(review.CoachResponse))
            {
                return BadRequest(new { error = "You have already responded to this review" });
            }

            // Add response
            review.CoachResponse = dto.Response;
            review.ResponseDate = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Response added successfully" });
        }

        // Helper method to update coach average rating
        private async Task UpdateCoachAverageRating(int coachId)
        {
            var coach = await _dbContext.Coaches.FindAsync(coachId);
            if (coach == null) return;

            var averageRating = await _dbContext.Reviews
                .Where(r => r.CoachID == coachId)
                .AverageAsync(r => (decimal?)r.Rating);

            coach.AvgRating = averageRating ?? 0;
            await _dbContext.SaveChangesAsync();
        }
    }
}