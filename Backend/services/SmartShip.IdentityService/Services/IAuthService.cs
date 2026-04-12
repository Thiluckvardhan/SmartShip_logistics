using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Services;

public interface IAuthService
{
    // Auth
    Task<(bool Ok, string? Message, object? Data)> RegisterAsync(RegisterDto request);
    Task<(bool Ok, string? Message, object? Data)> LoginAsync(LoginDto request);
    Task<(bool Ok, string? Message, object? Data)> RefreshTokenAsync(TokenDto request);
    Task<(bool Ok, string? Message)> LogoutAsync(TokenDto request);
    Task<(bool Ok, string? Message, object? Data)> ForgotPasswordAsync(ForgotPasswordDto request);
    Task<(bool Ok, string? Message)> ResetPasswordAsync(ResetPasswordDto request);

    // Users - Current User (any authenticated)
    Task<object?> GetCurrentUserAsync(Guid userId);
    Task<(bool Ok, string? Message, object? Data)> UpdateCurrentUserAsync(Guid userId, UpdateUserDto request);
    Task<bool> DeleteCurrentUserAsync(Guid userId);

    // Users - Admin Only
    Task<object> GetAllUsersAsync();
    Task<object?> GetUserByIdAsync(Guid userId);
    Task<object?> GetUserByEmailAsync(string email);
    Task<(bool Ok, string? Message)> UpdateUserRoleAsync(Guid userId, string roleName);
    Task<(bool Ok, string? Message)> UpdateUserRoleByEmailAsync(string email, string roleName);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> DeleteUserByEmailAsync(string email);

    // Roles
    Task<object> GetRolesAsync();
    Task<(bool Ok, string? Message, object? Data)> CreateRoleAsync(CreateRoleDto request);
    Task<object?> GetRoleAsync(int id);
    Task<(bool Ok, string? Message)> DeleteRoleAsync(int id);
}