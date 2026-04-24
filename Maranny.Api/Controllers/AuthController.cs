using Google.Apis.Auth;
using Maranny.Application.DTOs.Auth;
using Maranny.Application.Interfaces;
using Maranny.Core.Entities;
using Maranny.Core.Enums;
using Maranny.Infrastructure.Data;
using Maranny.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Maranny.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IJwtService jwtService,
            IEmailService emailService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _authService = authService;
            _userManager = userManager;
            _dbContext = dbContext;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
        }

        // ─────────────────────────────────────────────────────────────
        // 1.1 REGISTER
        // FIX #1: Moved coach-specific validation BEFORE user creation
        //         so we never create an orphaned user in the DB.
        // FIX #2: Removed token generation — registration must NOT
        //         auto-login. Returns a success message only.
        // FIX #3: Removed Password = "" from Client entity (bad practice).
        // ─────────────────────────────────────────────────────────────
        [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var (success, message, data) = await _authService.RegisterAsync(
            dto, Request.Scheme, Request.Host.ToString());

        if (!success) return BadRequest(new { error = message });
        return Ok(new { message, user = data });
    }

    [HttpPost("coach-onboarding/complete")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteCoachOnboarding(CompleteCoachOnboardingDto dto)
        {
            var user = await _userManager.Users
                .Include(u => u.Coach)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || user.Coach == null || user.PrimaryUserType != UserType.Coach)
            {
                return NotFound(new { error = "Coach account not found" });
            }

            if (user.IsBlocked)
            {
                return StatusCode(403, new
                {
                    error = "AccountBlocked",
                    message = "Your account has been blocked by an administrator.",
                    reason = user.BlockReason
                });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            var distinctSportIds = dto.Sports.Select(s => s.SportID).Distinct().ToList();
            var existingSports = await _dbContext.Sports
                .Where(s => distinctSportIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            var missingSportIds = distinctSportIds.Except(existingSports).ToList();
            if (missingSportIds.Count != 0)
            {
                return BadRequest(new
                {
                    error = "One or more selected sports do not exist",
                    sportIds = missingSportIds
                });
            }

            ApplyCoachOnboarding(user.Coach, dto);

            var existingCoachSports = await _dbContext.CoachSports
                .Where(cs => cs.CoachID == user.Coach.CoachID)
                .ToListAsync();
            _dbContext.CoachSports.RemoveRange(existingCoachSports);

            _dbContext.CoachSports.AddRange(dto.Sports.Select(s => new CoachSport
            {
                CoachID = user.Coach.CoachID,
                SportID = s.SportID,
                Description = s.Description?.Trim(),
                PricePerSession = dto.SessionPrice,
                ExperienceYears = dto.ExperienceYears
            }));

            var existingLocations = await _dbContext.CoachLocations
                .Where(cl => cl.CoachID == user.Coach.CoachID)
                .ToListAsync();
            _dbContext.CoachLocations.RemoveRange(existingLocations);
            _dbContext.CoachLocations.Add(new CoachLocation
            {
                CoachID = user.Coach.CoachID,
                WorkingLocation = dto.City.Trim()
            });

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = user.EmailConfirmed
                    ? "Coach profile completed successfully. You can now log in once your account is verified."
                    : "Coach profile completed successfully. Please confirm your email before logging in.",
                emailConfirmed = user.EmailConfirmed,
                verificationStatus = user.Coach.VerificationStatus.ToString()
            });
        }

    // ─────────────────────────────────────────────────────────────
    // 1.2 LOGIN
    // No structural bugs — response shape matches docs.
    // Minor: renamed attemptsLeft → attemptsRemaining to match docs.
    // ─────────────────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var (success, statusCode, message, data) = await _authService.LoginAsync(dto);

        if (!success) return StatusCode(statusCode, new { error = message });
        return Ok(data);
    }

    // ─────────────────────────────────────────────────────────────
    // 1.4 REFRESH TOKEN
    // No change to logic — kept accessToken requirement (valid pattern).
    // Fixed response shape to match docs (accessToken + refreshToken only).
    // ─────────────────────────────────────────────────────────────
    [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(RefreshTokenDto dto)
        {
            var userId = _jwtService.GetUserIdFromExpiredToken(dto.AccessToken);
            if (userId == null)
                return Unauthorized(new { error = "Invalid access token" });

            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.UserId == userId);

            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { error = "Invalid or expired refresh token" });

            var user = await _userManager.Users
                .Include(u => u.Client)
                .Include(u => u.Coach)
                .Include(u => u.Admin)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.IsBlocked)
                return Unauthorized(new { error = "User not found or blocked" });

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Rotate refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReplacedByToken = newRefreshToken;

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            // FIX: Return only tokens (no full user object) — matches docs
            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.5 LOGOUT
        // FIX: Changed [FromBody] string to a proper DTO to avoid
        //      Swagger/JSON deserialization issues with raw strings.
        // ─────────────────────────────────────────────────────────────
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.UserId == userId);

            if (token != null)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully" });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.6 GET CURRENT USER (/me)
        // FIX: Added verificationStatus for coaches so Flutter app knows
        //      if coach is Pending / Approved / Rejected.
        //      Added profilePicture field.
        // ─────────────────────────────────────────────────────────────
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _userManager.Users
                .Include(u => u.Client)
                .Include(u => u.Coach)
                .Include(u => u.Admin)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { error = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);

            CoachOnboardingState? coachOnboardingState = null;
            if (user.Coach != null)
            {
                coachOnboardingState = await BuildCoachOnboardingStateAsync(user.Coach);
            }

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.Client?.F_name ?? user.Coach?.F_name ?? user.Admin?.F_name ?? "",
                lastName = user.Client?.L_name ?? user.Coach?.L_name ?? user.Admin?.L_name ?? "",
                phoneNumber = user.PhoneNumber,
                userType = user.PrimaryUserType.ToString(),
                roles,
                emailConfirmed = user.EmailConfirmed,
                isBlocked = user.IsBlocked,
                profilePicture = user.Client?.URL ?? user.Coach?.URL,       
                verificationStatus = user.Coach != null
                    ? user.Coach.VerificationStatus.ToString()
                    : null,
                coachSetupCompleted = coachOnboardingState != null
                    ? coachOnboardingState.IsComplete
                    : (bool?)null
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.7 FORGOT PASSWORD
        // FIX: Removed resetToken from response body — CRITICAL security
        //      vulnerability. Code is now sent via email only.
        //      Added proper email sending call.
        // ─────────────────────────────────────────────────────────────
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Always return same message — prevents user enumeration
            if (user == null)
                return Ok(new { message = "If the email exists, a reset code has been sent" });

            // Generate 6-digit code
            var random = new Random();
            var resetCode = random.Next(100000, 999999).ToString();

            // Invalidate existing unused codes
            var existingTokens = await _dbContext.PasswordResetTokens
                .Where(prt => prt.UserId == user.Id && !prt.IsUsed)
                .ToListAsync();

            foreach (var t in existingTokens)
            {
                t.IsUsed = true;
                t.UsedAt = DateTime.UtcNow;
            }

            _dbContext.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            // FIX: Send code via email — NEVER expose in response
            try
            {
                var firstName = user.Client?.F_name ?? user.Coach?.F_name ?? user.Email;
                await _emailService.SendPasswordResetAsync(user.Email!, firstName!, resetCode);
            }
            catch (Exception)
            {
                // Log in production — don't expose error details
            }

            return Ok(new { message = "If the email exists, a reset code has been sent" });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.8 RESET PASSWORD
        // No bugs — logic is correct. Minor: unified error message.
        // ─────────────────────────────────────────────────────────────
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest(new { error = "Invalid email or token" });

            var resetToken = await _dbContext.PasswordResetTokens
                .FirstOrDefaultAsync(prt =>
                    prt.UserId == user.Id &&
                    prt.Token == dto.Token &&
                    !prt.IsUsed &&
                    prt.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
                return BadRequest(new { error = "Invalid or expired reset token" });

            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, identityToken, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;

            // Revoke all refresh tokens — force re-login on all devices
            var refreshTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync();

            foreach (var rt in refreshTokens)
            {
                rt.IsRevoked = true;
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.9 CHANGE PASSWORD
        // No bugs — logic is correct.
        // ─────────────────────────────────────────────────────────────
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(new { error = "User not found" });

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
                return BadRequest(new { error = "Current password is incorrect" });

            if (dto.CurrentPassword == dto.NewPassword)
                return BadRequest(new { error = "New password must be different from current password" });

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            // Revoke all refresh tokens
            var refreshTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully. Please login again." });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.10 CONFIRM EMAIL
        // No bugs — logic is correct.
        // ─────────────────────────────────────────────────────────────
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest(new { error = "Invalid confirmation token" });

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(new { error = "User not found" });

            if (user.EmailConfirmed)
                return Ok(new { message = "Email already confirmed" });

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return BadRequest(new { error = "Email confirmation failed", details = result.Errors.Select(e => e.Description) });

            return Ok(new { message = "Email confirmed successfully! You can now login." });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.11 RESEND CONFIRMATION EMAIL
        // FIX: "Email already confirmed" leaked user existence info.
        //      Now returns generic message for already-confirmed case too.
        // ─────────────────────────────────────────────────────────────
        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationDto dto)
        {
            // Generic message in all non-error cases — prevents user enumeration
            const string genericMessage = "If the email exists, a confirmation link has been sent";

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Ok(new { message = genericMessage });

            // FIX: Don't reveal already-confirmed status — return same generic message
            if (user.EmailConfirmed)
                return Ok(new { message = genericMessage });

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email" +
                                   $"?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

            try
            {
                var firstName = user.Client?.F_name ?? user.Coach?.F_name ?? user.Email;
                await _emailService.SendEmailConfirmationAsync(user.Email!, firstName!, confirmationLink);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Failed to send confirmation email" });
            }

            return Ok(new { message = genericMessage });
        }

        // ─────────────────────────────────────────────────────────────
        // 1.3 GOOGLE LOGIN
        // No critical bugs — logic matches docs.
        // Minor: Coach via Google skips NationalId (by design — noted).
        // ─────────────────────────────────────────────────────────────
        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["GoogleAuth:ClientId"]! }
                    });

                if (payload == null)
                    return Unauthorized(new { error = "Invalid Google token" });

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        EmailConfirmed = true, // Google already verified
                        PrimaryUserType = dto.UserType == "Coach" ? UserType.Coach : UserType.Client,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

                    if (dto.UserType == "Coach")
                    {
                        _dbContext.Coaches.Add(new Coach
                        {
                            UserId = user.Id,
                            F_name = payload.GivenName ?? payload.Email.Split('@')[0],
                            L_name = payload.FamilyName ?? "",
                            VerificationStatus = VerificationStatus.Pending,
                            ExperienceYears = 0
                        });
                    }
                    else
                    {
                        _dbContext.Clients.Add(new Client
                        {
                            UserId = user.Id,
                            F_name = payload.GivenName ?? payload.Email.Split('@')[0],
                            L_name = payload.FamilyName ?? "",
                            Email = payload.Email
                        });
                        await _userManager.AddToRoleAsync(user, "Client");
                    }

                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    if (user.IsBlocked)
                    {
                        return StatusCode(403, new
                        {
                            error = "AccountBlocked",
                            message = "Your account has been blocked by an administrator.",
                            reason = user.BlockReason
                        });
                    }
                }

                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _jwtService.GenerateAccessToken(user, roles.ToList());
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                _dbContext.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();

                // Reload with profiles
                user = await _userManager.Users
                    .Include(u => u.Client)
                    .Include(u => u.Coach)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                return Ok(new
                {
                    accessToken,
                    refreshToken = newRefreshToken,
                    user = new
                    {
                        id = user!.Id,
                        email = user.Email,
                        firstName = user.Client?.F_name ?? user.Coach?.F_name ?? payload.GivenName,
                        lastName = user.Client?.L_name ?? user.Coach?.L_name ?? payload.FamilyName,
                        userType = user.PrimaryUserType.ToString(),
                        roles
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Google authentication failed", details = ex.Message });
            }
        }

        private static void ApplyCoachOnboarding(Coach coach, CompleteCoachOnboardingDto dto)
        {
            var parts = dto.FullName.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 1)
            {
                coach.F_name = parts[0];
            }
            else
            {
                coach.F_name = parts[0];
                coach.L_name = string.Join(" ", parts.Skip(1));
            }

            coach.ID = dto.NationalId.Trim();
            coach.Bio = dto.Bio?.Trim();
            coach.ExperienceYears = dto.ExperienceYears;
            coach.CertificateUrl = dto.CertificateUrl?.Trim();
            coach.AvailabilityStatus = string.Join(",",
                dto.AvailableDays
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => d.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private async Task<CoachOnboardingState> BuildCoachOnboardingStateAsync(Coach coach)
        {
            var sportRows = await _dbContext.CoachSports
                .Where(cs => cs.CoachID == coach.CoachID)
                .ToListAsync();

            var hasLocation = await _dbContext.CoachLocations
                .AnyAsync(cl => cl.CoachID == coach.CoachID && !string.IsNullOrWhiteSpace(cl.WorkingLocation));

            var hasFullName = !string.IsNullOrWhiteSpace(coach.F_name);
            var hasNationalId = !string.IsNullOrWhiteSpace(coach.ID);
            var hasAvailableDays = !string.IsNullOrWhiteSpace(coach.AvailabilityStatus);
            var hasExperienceYears = coach.ExperienceYears.HasValue;
            var hasSportSelection = sportRows.Count > 0;
            var hasSessionPrice = sportRows.Any(cs => cs.PricePerSession > 0);

            return new CoachOnboardingState(
                hasFullName,
                hasNationalId,
                hasAvailableDays,
                hasExperienceYears,
                hasSportSelection,
                hasLocation,
                hasSessionPrice);
        }

        private sealed record CoachOnboardingState(
            bool HasFullName,
            bool HasNationalId,
            bool HasAvailableDays,
            bool HasExperienceYears,
            bool HasSportSelection,
            bool HasLocation,
            bool HasSessionPrice)
        {
            public bool IsComplete =>
                HasFullName &&
                HasNationalId &&
                HasAvailableDays &&
                HasExperienceYears &&
                HasSportSelection &&
                HasLocation &&
                HasSessionPrice;
        }
    }
}
