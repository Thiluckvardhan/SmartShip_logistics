using SmartShip.IdentityService.Models;

namespace SmartShip.IdentityService.Repositories;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, Guid? exceptUserId = null);
    Task<User?> GetUserByEmailAsync(string email, bool includeRole = false);
    Task<User?> GetUserByIdAsync(Guid userId, bool includeRoleAndUserRoles = false);
    Task<List<User>> GetUsersAsync();
    Task AddUserAsync(User user);
    Task DeleteUserAsync(User user);

    Task<Role?> GetRoleByIdAsync(int roleId);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<Role?> GetFirstRoleAsync();
    Task<List<Role>> GetRolesAsync();
    Task<Role?> GetRoleAsync(int roleId);
    Task AddRoleAsync(Role role);
    Task DeleteRoleAsync(Role role);

    Task<bool> UserRoleExistsAsync(Guid userId, int roleId);
    Task AddUserRoleAsync(UserRole userRole);
    Task<UserRole?> GetUserRoleAsync(Guid userId, int roleId);
    Task<List<UserRole>> GetUserRolesByUserIdAsync(Guid userId);
    Task RemoveUserRoleAsync(UserRole userRole);
    Task ReplaceUserRoleAsync(Guid userId, int roleId);

    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string hash, bool includeUserAndRole = false);

    Task AddPasswordResetTokenAsync(PasswordResetToken resetToken);
    Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string hash, bool includeUser = false);

    Task SaveChangesAsync();
}