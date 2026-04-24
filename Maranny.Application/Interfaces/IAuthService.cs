using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Maranny.Application.DTOs.Auth;

namespace Maranny.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool success, string message, object? data)> RegisterAsync(RegisterDto dto, string scheme, string host);
        Task<(bool success, int statusCode, string message, object? data)> LoginAsync(LoginDto dto);
        Task<(bool success, string message)> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<(bool success, string message)> ResetPasswordAsync(ResetPasswordDto dto);
        Task<(bool success, string message)> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<(bool success, string message)> LogoutAsync(LogoutDto dto);
        Task<(bool success, string message, object? data)> RefreshTokenAsync(RefreshTokenDto dto);
        Task<(bool success, string message)> ResendConfirmationAsync(ResendConfirmationDto dto);
        Task<(bool success, string message)> ConfirmEmailAsync(string userId, string token);
    }
}