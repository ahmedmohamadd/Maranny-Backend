using Maranny.Application.DTOs.Sessions;
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
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public SessionsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Coach creates a training session
        [HttpPost]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> CreateSession(CreateSessionDto dto)
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

            if (coach.VerificationStatus != VerificationStatus.Verified &&
                coach.VerificationStatus != VerificationStatus.Approved)
            {
                return BadRequest(new { error = "Coach must be verified before creating sessions" });
            }

            // Validate session date is in the future
            if (dto.SessionDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new { error = "Cannot create session in the past" });
            }

            // Validate end time is after start time
            if (dto.End_Time <= dto.Start_Time)
            {
                return BadRequest(new { error = "End time must be after start time" });
            }

            // Verify sport exists
            var sport = await _dbContext.Sports.FindAsync(dto.SportID);
            if (sport == null)
            {
                return NotFound(new { error = "Sport not found" });
            }

            // Check for overlapping sessions for this coach
            var overlappingSession = await _dbContext.TrainingSessions
                .Where(s => s.CoachID == coach.CoachID &&
                           s.SessionDate.Date == dto.SessionDate.Date &&
                           s.Status != SessionStatus.Cancelled &&
                           ((dto.Start_Time >= s.Start_Time && dto.Start_Time < s.End_Time) ||
                            (dto.End_Time > s.Start_Time && dto.End_Time <= s.End_Time) ||
                            (dto.Start_Time <= s.Start_Time && dto.End_Time >= s.End_Time)))
                .FirstOrDefaultAsync();

            if (overlappingSession != null)
            {
                return BadRequest(new { error = "You have an overlapping session at this time" });
            }

            // Create session
            var session = new TrainingSession
            {
                CoachID = coach.CoachID,
                SportID = dto.SportID,
                SessionDate = dto.SessionDate,
                SessionType = dto.SessionType,
                Location = dto.Location,
                MaxParticipants = dto.MaxParticipants,
                Start_Time = dto.Start_Time,
                End_Time = dto.End_Time,
                Status = SessionStatus.Scheduled,
            };

            _dbContext.TrainingSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Session created successfully",
                sessionId = session.SessionID
            });
        }

        // Get my sessions (as coach)
        [HttpGet("my")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetMySessions(
    [FromQuery] string? status = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
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

            // Get sessions
            var query = _dbContext.TrainingSessions
                .Include(s => s.Sport)
                .Where(s => s.CoachID == coach.CoachID);

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<SessionStatus>(status, out var sessionStatus))
            {
                query = query.Where(s => s.Status == sessionStatus);
            }

            var totalCount = await query.CountAsync();

            var sessions = await query
    .OrderByDescending(s => s.SessionDate)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(s => new
    {
        s.SessionID,
        s.SessionDate,
        s.SessionType,
        s.Location,
        s.MaxParticipants,
        s.Start_Time,
        s.End_Time,
        Status = s.Status.ToString(),
        SportName = s.Sport.Name,
        SportID = s.SportID,
        Price = _dbContext.CoachSports
            .Where(cs => cs.CoachID == s.CoachID && cs.SportID == s.SportID)
            .Select(cs => cs.PricePerSession)
            .FirstOrDefault(),
        BookedCount = _dbContext.ClientSessions.Count(cs => cs.SessionID == s.SessionID),
        AvailableSlots = s.MaxParticipants - _dbContext.ClientSessions.Count(cs => cs.SessionID == s.SessionID)
    })
    .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                sessions
            });
        }
            // Browse available sessions (public/client)
            [HttpGet]
            [AllowAnonymous]
            public async Task<IActionResult> GetAvailableSessions(
            [FromQuery] int? coachId = null,
            [FromQuery] int? sportId = null,
            [FromQuery] DateTime? date = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            {
                var query = _dbContext.TrainingSessions
                    .Include(s => s.Sport)
                    .Include(s => s.Coach)
                    .Where(s => s.Status == SessionStatus.Scheduled &&
                               s.SessionDate >= DateTime.UtcNow.Date);

                // Filter by coach
                if (coachId.HasValue)
                {
                    query = query.Where(s => s.CoachID == coachId.Value);
                }

                // Filter by sport
                if (sportId.HasValue)
                {
                    query = query.Where(s => s.SportID == sportId.Value);
                }

                // Filter by date
                if (date.HasValue)
                {
                    query = query.Where(s => s.SessionDate.Date == date.Value.Date);
                }

                query = query.Where(s =>
                    !s.MaxParticipants.HasValue ||
                    _dbContext.ClientSessions.Count(cs => cs.SessionID == s.SessionID) < s.MaxParticipants.Value);

                var totalCount = await query.CountAsync();

                var sessions = await query
                    .OrderBy(s => s.SessionDate)
                    .ThenBy(s => s.Start_Time)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.SessionID,
                        s.SessionDate,
                        s.SessionType,
                        s.Location,
                        s.MaxParticipants,
                        s.Start_Time,
                        s.End_Time,
                        SportName = s.Sport.Name,
                        SportID = s.SportID,
                        Price = _dbContext.CoachSports
                            .Where(cs => cs.CoachID == s.CoachID && cs.SportID == s.SportID)
                            .Select(cs => cs.PricePerSession)
                            .FirstOrDefault(),
                        Coach = new
                        {
                            s.Coach.CoachID,
                            Name = s.Coach.F_name + " " + s.Coach.L_name,
                            s.Coach.AvgRating,
                            s.Coach.ExperienceYears,
                            VerificationStatus = s.Coach.VerificationStatus.ToString()
                        },
                        AvailableSlots = s.MaxParticipants - _dbContext.ClientSessions.Count(cs => cs.SessionID == s.SessionID)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    sessions
                });
            }

            // Update session (coach only)
            [HttpPut("{sessionId}")]
            [Authorize(Roles = "Coach")]
            public async Task<IActionResult> UpdateSession(int sessionId, UpdateSessionDto dto)
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

                // Get session
                var session = await _dbContext.TrainingSessions.FindAsync(sessionId);
                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                // Verify ownership
                if (session.CoachID != coach.CoachID)
                {
                    return Forbid();
                }

                // Update fields (only if provided)
                if (dto.SessionDate.HasValue)
                    session.SessionDate = dto.SessionDate.Value;

                if (!string.IsNullOrEmpty(dto.SessionType))
                    session.SessionType = dto.SessionType;

                if (!string.IsNullOrEmpty(dto.Location))
                    session.Location = dto.Location;

                if (dto.MaxParticipants.HasValue)
                    session.MaxParticipants = dto.MaxParticipants.Value;

                if (dto.Start_Time.HasValue)
                    session.Start_Time = dto.Start_Time.Value;

                if (dto.End_Time.HasValue)
                    session.End_Time = dto.End_Time.Value;

                if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<SessionStatus>(dto.Status, out var status))
                    session.Status = status;

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Session updated successfully" });
            }

            // Delete/Cancel session (coach only)
            [HttpDelete("{sessionId}")]
            [Authorize(Roles = "Coach")]
            public async Task<IActionResult> CancelSession(int sessionId)
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

                // Get session
                var session = await _dbContext.TrainingSessions.FindAsync(sessionId);
                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                // Verify ownership
                if (session.CoachID != coach.CoachID)
                {
                    return Forbid();
                }

                // Mark as cancelled
                session.Status = SessionStatus.Cancelled;
                await _dbContext.SaveChangesAsync();

                // TODO: Notify booked clients about cancellation

                return Ok(new { message = "Session cancelled successfully" });
            }
        }
    } 
