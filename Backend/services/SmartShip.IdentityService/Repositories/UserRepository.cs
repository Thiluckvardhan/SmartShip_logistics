using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Data;
using SmartShip.IdentityService.Models;

namespace SmartShip.IdentityService.Repositories;

public class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email, Guid? exceptUserId = null)
        => dbContext.Users.AnyAsync(x => x.Email == email && (!exceptUserId.HasValue || x.UserId != exceptUserId.Value));

    public Task<User?> GetUserByEmailAsync(string email, bool includeRole = false)
    {
        var query = dbContext.Users.AsQueryable();
        if (includeRole)
        {
            query = query.Include(x => x.UserRoles)
                .ThenInclude(x => x.Role);
        }

        return query.FirstOrDefaultAsync(x => x.Email == email);
    }

    public Task<User?> GetUserByIdAsync(Guid userId, bool includeRoleAndUserRoles = false)
    {
        var query = dbContext.Users.AsQueryable();
        if (includeRoleAndUserRoles)
        {
            query = query.Include(x => x.UserRoles)
                .ThenInclude(x => x.Role);
        }
        return query.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public Task<List<User>> GetUsersAsync() => dbContext.Users
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .ToListAsync();

    public Task<List<User>> GetUsersPagedAsync(int pageNumber, int pageSize) => dbContext.Users
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .OrderByDescending(x => x.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    public Task<int> GetUserCountAsync() => dbContext.Users.CountAsync();

    public async Task AddUserAsync(User user) => await dbContext.Users.AddAsync(user);

    public Task DeleteUserAsync(User user)
    {
        dbContext.Users.Remove(user);
        return Task.CompletedTask;
    }

    public Task<Role?> GetRoleByIdAsync(int roleId) => dbContext.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId);

    public Task<Role?> GetRoleByNameAsync(string roleName) => dbContext.Roles.FirstOrDefaultAsync(x => x.RoleName == roleName);

    public Task<Role?> GetFirstRoleAsync() => dbContext.Roles.OrderBy(x => x.RoleName).FirstOrDefaultAsync();

    public Task<List<Role>> GetRolesAsync() => dbContext.Roles.ToListAsync();

    public Task<Role?> GetRoleAsync(int roleId) => dbContext.Roles
        .Include(x => x.UserRoles)
        .FirstOrDefaultAsync(x => x.RoleId == roleId);

    public async Task AddRoleAsync(Role role) => await dbContext.Roles.AddAsync(role);

    public Task DeleteRoleAsync(Role role)
    {
        dbContext.Roles.Remove(role);
        return Task.CompletedTask;
    }

    public Task<bool> UserRoleExistsAsync(Guid userId, int roleId) => dbContext.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId);

    public async Task AddUserRoleAsync(UserRole userRole) => await dbContext.UserRoles.AddAsync(userRole);

    public Task<UserRole?> GetUserRoleAsync(Guid userId, int roleId) => dbContext.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);

    public Task<List<UserRole>> GetUserRolesByUserIdAsync(Guid userId) => dbContext.UserRoles
        .Where(x => x.UserId == userId)
        .Include(x => x.Role)
        .ToListAsync();

    public Task RemoveUserRoleAsync(UserRole userRole)
    {
        dbContext.UserRoles.Remove(userRole);
        return Task.CompletedTask;
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken) => await dbContext.RefreshTokens.AddAsync(refreshToken);

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string hash, bool includeUserAndRole = false)
    {
        var query = dbContext.RefreshTokens.AsQueryable();
        if (includeUserAndRole)
        {
            query = query
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role);
        }

        return query.FirstOrDefaultAsync(x => x.TokenHash == hash);
    }

    public async Task AddPasswordResetTokenAsync(PasswordResetToken resetToken) => await dbContext.PasswordResetTokens.AddAsync(resetToken);

    public Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string hash, bool includeUser = false)
    {
        var query = dbContext.PasswordResetTokens.AsQueryable();
        if (includeUser) query = query.Include(x => x.User);
        return query.FirstOrDefaultAsync(x => x.TokenHash == hash);
    }

    public async Task ReplaceUserRoleAsync(Guid userId, int roleId)
    {
        var existing = await dbContext.UserRoles.Where(x => x.UserId == userId).ToListAsync();
        if (existing.Count != 0)
        {
            dbContext.UserRoles.RemoveRange(existing);
        }

        await dbContext.UserRoles.AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        });
    }

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}