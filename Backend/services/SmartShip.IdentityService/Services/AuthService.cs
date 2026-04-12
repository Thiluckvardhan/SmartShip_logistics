using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Models;
using SmartShip.IdentityService.Repositories;

namespace SmartShip.IdentityService.Services;

public class AuthService(IUserRepository repository, IConfiguration configuration) : IAuthService
{
    private readonly string _jwtKey = configuration["Jwt:Key"] ?? "SmartShip.SuperSecret.Key.For.Dev.Only.2026";
    private readonly string _jwtIssuer = configuration["Jwt:Issuer"] ?? "SmartShip";
    private readonly string[] _jwtAudiences =
        configuration.GetSection("Jwt:Audiences").Get<string[]>()?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        ?? [configuration["Jwt:Audience"] ?? "SmartShipClients"];

    public async Task<(bool Ok, string? Message, object? Data)> RegisterAsync(RegisterDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await repository.EmailExistsAsync(email))
        {
            return (false, "Email already registered.", null);
        }

        var role = await repository.GetRoleByNameAsync("Customer") ?? await repository.GetFirstRoleAsync();
        if (role is null)
        {
            role = new Role { RoleName = "Customer" };
            await repository.AddRoleAsync(role);
            await repository.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            PasswordHash = HashToken(request.Password),
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddUserAsync(user);
        await repository.SaveChangesAsync();

        await repository.ReplaceUserRoleAsync(user.UserId, role.RoleId);
        await repository.SaveChangesAsync();

        return (true, null, new { user.Name, user.Email, user.Phone, Role = role.RoleName });
    }

    public async Task<(bool Ok, string? Message, object? Data)> LoginAsync(LoginDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await repository.GetUserByEmailAsync(email, includeRole: true);
        if (user is null || user.PasswordHash != HashToken(request.Password))
        {
            return (false, "Invalid credentials.", null);
        }

        var refreshToken = GenerateToken();
        await repository.AddRefreshTokenAsync(new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        await repository.SaveChangesAsync();
        return (true, null, new { accessToken = GenerateAccessToken(user), refreshToken });
    }

    public async Task<(bool Ok, string? Message, object? Data)> RefreshTokenAsync(TokenDto request)
    {
        var token = await repository.GetRefreshTokenByHashAsync(HashToken(request.Token), includeUserAndRole: true);
        if (token is null || token.IsRevoked || token.ExpiresAt <= DateTime.UtcNow)
        {
            return (false, "Invalid refresh token.", null);
        }

        token.IsRevoked = true;
        var newRefreshToken = GenerateToken();
        await repository.AddRefreshTokenAsync(new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = token.UserId,
            TokenHash = HashToken(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        await repository.SaveChangesAsync();
        return (true, null, new { accessToken = GenerateAccessToken(token.User), refreshToken = newRefreshToken });
    }

    public async Task<(bool Ok, string? Message)> LogoutAsync(TokenDto request)
    {
        var token = await repository.GetRefreshTokenByHashAsync(HashToken(request.Token));
        if (token is null) return (false, "Token not found.");
        token.IsRevoked = true;
        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Message, object? Data)> ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await repository.GetUserByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return (true, "If the email exists, a reset token was created.", null);
        }

        var resetToken = GenerateToken();
        await repository.AddPasswordResetTokenAsync(new PasswordResetToken
        {
            PasswordResetTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            TokenHash = HashToken(resetToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false
        });

        await repository.SaveChangesAsync();
        return (true, "Reset token generated.", new { resetToken });
    }

    public async Task<(bool Ok, string? Message)> ResetPasswordAsync(ResetPasswordDto request)
    {
        var resetToken = await repository.GetPasswordResetTokenByHashAsync(HashToken(request.Token), includeUser: true);
        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            return (false, "Invalid or expired token.");
        }

        resetToken.IsUsed = true;
        resetToken.User.PasswordHash = HashToken(request.NewPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<object?> GetCurrentUserAsync(Guid userId)
    {
        var user = await repository.GetUserByIdAsync(userId, includeRoleAndUserRoles: true);
        return user is null ? null : new
        {
            user.Name,
            user.Email,
            user.Phone,
            Role = user.UserRoles.Select(x => x.Role.RoleName).FirstOrDefault() ?? "Customer"
        };
    }

    public async Task<object> GetUsersAsync()
    {
        var users = await repository.GetUsersAsync();
        return users.Select(x => new
        {
            x.Name,
            x.Email,
            x.Phone,
            Role = x.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Customer",
            x.CreatedAt,
            x.UpdatedAt
        }).ToList();
    }

    public async Task<(bool Ok, string? Message, object? Data)> UpdateCurrentUserAsync(Guid userId, UpdateUserDto request)
    {
        var user = await repository.GetUserByIdAsync(userId);
        if (user is null) return (false, "User not found.", null);

        if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name.Trim();
        if (request.Phone is not null) user.Phone = request.Phone.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync();
        return (true, null, new { user.Name, user.Email, user.Phone, user.UpdatedAt });
    }

    public async Task<bool> DeleteCurrentUserAsync(Guid userId)
    {
        var user = await repository.GetUserByIdAsync(userId);
        if (user is null) return false;
        await repository.DeleteUserAsync(user);
        await repository.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetRolesAsync()
    {
        var roles = await repository.GetRolesAsync();
        return roles.Select(x => new { x.RoleId, x.RoleName }).ToList();
    }

    public async Task<(bool Ok, string? Message, object? Data)> CreateRoleAsync(CreateRoleDto request)
    {
        var roleName = request.RoleName.Trim();
        if (await repository.GetRoleByNameAsync(roleName) is not null)
        {
            return (false, "Role already exists.", null);
        }

        var role = new Role { RoleName = roleName };
        await repository.AddRoleAsync(role);
        await repository.SaveChangesAsync();
        return (true, null, new { role.RoleId, role.RoleName });
    }

    public Task<object?> GetRoleAsync(int id) => repository.GetRoleAsync(id).ContinueWith(t => 
        t.Result is null ? null : (object)new { t.Result.RoleId, t.Result.RoleName });

    public async Task<(bool Ok, string? Message)> DeleteRoleAsync(int id)
    {
        var role = await repository.GetRoleAsync(id);
        if (role is null) return (false, "Role not found.");

        if (role.UserRoles.Any())
        {
            return (false, "Cannot delete role with assigned users.");
        }

        await repository.DeleteRoleAsync(role);
        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Message, object? Data)> AssignUserRoleAsync(AssignUserRoleDto request)
    {
        var user = await repository.GetUserByEmailAsync(request.Email.Trim().ToLowerInvariant());
        var role = await repository.GetRoleByNameAsync(request.RoleName.Trim());
        if (user is null || role is null)
        {
            return (false, "User or role not found.", null);
        }

        if (await repository.UserRoleExistsAsync(user.UserId, role.RoleId))
        {
            return (false, "Role already assigned.", null);
        }

        await repository.AddUserRoleAsync(new UserRole { UserId = user.UserId, RoleId = role.RoleId, AssignedAt = DateTime.UtcNow });
        await repository.SaveChangesAsync();
        return (true, null, new { user.Email, role.RoleName });
    }

    public async Task<object> GetCurrentUserRolesAsync(Guid userId)
        => (await repository.GetUserRolesByUserIdAsync(userId)).Select(x => new { x.Role.RoleName, x.AssignedAt }).ToList();

    public async Task<(bool Ok, string? Message)> RemoveUserRoleAsync(AssignUserRoleDto request)
    {
        var user = await repository.GetUserByEmailAsync(request.Email.Trim().ToLowerInvariant());
        var role = await repository.GetRoleByNameAsync(request.RoleName.Trim());
        if (user is null || role is null) return (false, "User or role not found.");

        var userRole = await repository.GetUserRoleAsync(user.UserId, role.RoleId);
        if (userRole is null) return (false, "User role not found.");

        await repository.RemoveUserRoleAsync(userRole);
        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<object> GetAllUsersAsync()
    {
        var users = await repository.GetUsersAsync();
        return users.Select(x => new
        {
            x.UserId,
            x.Name,
            x.Email,
            x.CreatedAt,
            Role = x.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Customer"
        }).ToList();
    }

    public async Task<object?> GetUserByIdAsync(Guid userId)
    {
        var user = await repository.GetUserByIdAsync(userId, includeRoleAndUserRoles: true);
        return user is null ? null : new
        {
            user.UserId,
            user.Name,
            user.Email,
            user.Phone,
            user.CreatedAt,
            user.UpdatedAt,
            Role = user.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Customer"
        };
    }

    public async Task<(bool Ok, string? Message)> UpdateUserRoleAsync(Guid userId, string roleName)
    {
        var user = await repository.GetUserByIdAsync(userId, includeRoleAndUserRoles: true);
        if (user is null) return (false, "User not found.");

        var role = await repository.GetRoleByNameAsync(roleName.Trim());
        if (role is null) return (false, "Role not found.");

        var currentRoleId = user.UserRoles.Select(x => x.RoleId).FirstOrDefault();
        if (currentRoleId == role.RoleId)
        {
            return (false, "User already has this role.");
        }

        await repository.ReplaceUserRoleAsync(userId, role.RoleId);
        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await repository.GetUserByIdAsync(userId);
        if (user is null) return false;
        await repository.DeleteUserAsync(user);
        await repository.SaveChangesAsync();
        return true;
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name)
        };

        var role = user.UserRoles
            .Select(x => x.Role.RoleName)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Customer";

        claims.Add(new Claim(ClaimTypes.Role, role));
        claims.AddRange(_jwtAudiences.Select(audience => new Claim(JwtRegisteredClaimNames.Aud, audience)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwtIssuer, claims: claims, expires: DateTime.UtcNow.AddHours(4), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public async Task<object?> GetUserByEmailAsync(string email)
    {
        var user = await repository.GetUserByEmailAsync(email.Trim().ToLowerInvariant(), includeRole: true);
        return user is null ? null : new
        {
            user.UserId,
            user.Name,
            user.Email,
            user.Phone,
            user.CreatedAt,
            user.UpdatedAt,
            Role = user.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Customer"
        };
    }

    public async Task<(bool Ok, string? Message)> UpdateUserRoleByEmailAsync(string email, string roleName)
    {
        var user = await repository.GetUserByEmailAsync(email.Trim().ToLowerInvariant(), includeRole: true);
        if (user is null) return (false, "User not found.");

        return await UpdateUserRoleAsync(user.UserId, roleName);
    }

    public async Task<bool> DeleteUserByEmailAsync(string email)
    {
        var user = await repository.GetUserByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is null) return false;

        await repository.DeleteUserAsync(user);
        await repository.SaveChangesAsync();
        return true;
    }
}