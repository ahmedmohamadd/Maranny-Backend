using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Maranny.Application.DTOs.Auth;
using Maranny.Application.Interfaces;
using Maranny.Core.Entities;
using Maranny.Core.Enums;
using Maranny.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Maranny.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IJwtService _jwtService;
        private readonly IEmailValidationService _emailValidationService;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IJwtService jwtService,
            IEmailValidationService emailValidationService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _jwtService = jwtService;
            _emailValidationService = emailValidationService;
            _emailService = emailService;
        }

        public async Task<(bool success, string message, object? data)> RegisterAsync(
            RegisterDto dto, string scheme, string host)
        {
            dto.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? null : dto.PhoneNumber.Trim();

            var emailValidation = await _emailValidationService.ValidateEmailDetailed(dto.Email);
            if (!emailValidation.isValid)
                return (false, emailValidation.reason, null);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return (false, "Email is already registered", null);

            var isCoach = dto.UserType.Equals("Coach", StringComparison.OrdinalIgnoreCase);
            if (isCoach && dto.IsCertified && string.IsNullOrWhiteSpace(dto.CertificateImageUrl))
                return (false, "Certificate image is required for certified coaches", null);

            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PrimaryUserType = isCoach ? UserType.Coach : UserType.Client,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);

            if (isCoach)
            {
                var coach = new Coach
                {
                    UserId = user.Id,
                    F_name = dto.FirstName,
                    L_name = dto.LastName,
                    VerificationStatus = VerificationStatus.Pending,
                    ExperienceYears = 0,
                    NationalIdImageUrl = dto.NationalIdImageUrl ?? string.Empty,
                    IsCertified = dto.IsCertified,
                    CertificateImageUrl = dto.CertificateImageUrl
                };
                _dbContext.Coaches.Add(coach);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Client");
                var client = new Client
                {
                    UserId = user.Id,
                    F_name = dto.FirstName,
                    L_name = dto.LastName,
                    Email = dto.Email
                };
                _dbContext.Clients.Add(client);
            }

            await _dbContext.SaveChangesAsync();

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = $"{scheme}://{host}/api/auth/confirm-email" +
                                       $"?userId={user.Id}&token={Uri.EscapeDataString(token)}";
                await _emailService.SendEmailConfirmationAsync(user.Email!, dto.FirstName, confirmationLink);
            }
            catch { }

            var data = new
            {
                id = user.Id,
                email = user.Email,
                firstName = dto.FirstName,
                lastName = dto.LastName,
                userType = user.PrimaryUserType.ToString()
            };

            var message = isCoach
                ? "Registration successful. Complete your coach profile, then confirm your email."
                : "Registration successful. Please check your email to confirm your account.";

            return (true, message, data);
        }

        public async Task<(bool success, int statusCode, string message, object? data)> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.Users
                .Include(u => u.Client)
                .Include(u => u.Coach)
                .Include(u => u.Admin)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return (false, 401, "Invalid email or password", null);

            if (user.IsBlocked)
                return (false, 403, "Your account has been blocked by an administrator.", null);

            if (!user.EmailConfirmed)
                return (false, 403, "Please confirm your email address before logging in.", null);

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var minutesLeft = (lockoutEnd - DateTimeOffset.UtcNow)?.TotalMinutes ?? 0;
                return (false, 403, $"Account locked. Try again in {Math.Ceiling(minutesLeft)} minutes", null);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                    return (false, 403, "Account locked. Try again in 15 minutes", null);

                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                var attemptsRemaining = Math.Max(0, 5 - failedAttempts);
                return (false, 401, $"Invalid credentials. {attemptsRemaining} attempts remaining", null);
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            var data = new
            {
                accessToken,
                refreshToken,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.Client?.F_name ?? user.Coach?.F_name ?? user.Admin?.F_name ?? "",
                    lastName = user.Client?.L_name ?? user.Coach?.L_name ?? user.Admin?.L_name ?? "",
                    userType = user.PrimaryUserType.ToString(),
                    roles
                }
            };

            return (true, 200, "Login successful", data);
        }

        public Task<(bool success, string message)> ForgotPasswordAsync(ForgotPasswordDto dto)
            => throw new NotImplementedException();

        public Task<(bool success, string message)> ResetPasswordAsync(ResetPasswordDto dto)
            => throw new NotImplementedException();

        public Task<(bool success, string message)> ChangePasswordAsync(string userId, ChangePasswordDto dto)
            => throw new NotImplementedException();

        public Task<(bool success, string message)> LogoutAsync(LogoutDto dto)
            => throw new NotImplementedException();

        public Task<(bool success, string message, object? data)> RefreshTokenAsync(RefreshTokenDto dto)
            => throw new NotImplementedException();

        public Task<(bool success, string message)> ResendConfirmationAsync(ResendConfirmationDto dto)
            => throw new NotImplementedException();

        public Task<(bool success, string message)> ConfirmEmailAsync(string userId, string token)
            => throw new NotImplementedException();
    }
}