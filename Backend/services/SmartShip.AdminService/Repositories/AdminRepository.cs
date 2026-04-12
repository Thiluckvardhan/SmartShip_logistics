using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Data;
using SmartShip.AdminService.Models;

namespace SmartShip.AdminService.Repositories;

public class AdminRepository(AdminDbContext dbContext) : IAdminRepository
{
    // ── Exceptions ──────────────────────────────────────────

    public async Task AddExceptionAsync(ExceptionRecord record) => await dbContext.ExceptionRecords.AddAsync(record);

    public Task<List<ExceptionRecord>> GetExceptionsByShipmentAsync(Guid shipmentId) => dbContext.ExceptionRecords
        .Where(x => x.ShipmentId == shipmentId)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<List<ExceptionRecord>> GetAllExceptionsAsync() => dbContext.ExceptionRecords
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<List<ExceptionRecord>> GetExceptionsPagedAsync(int pageNumber, int pageSize) => dbContext.ExceptionRecords
        .OrderByDescending(x => x.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    public Task<List<ExceptionRecord>> GetOpenExceptionsAsync() => dbContext.ExceptionRecords
        .Where(x => x.Status == "Open")
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<ExceptionRecord?> GetExceptionAsync(Guid id) => dbContext.ExceptionRecords.FirstOrDefaultAsync(x => x.ExceptionId == id);

    public Task<int> GetExceptionCountAsync() => dbContext.ExceptionRecords.CountAsync();

    public Task<int> GetExceptionCountByStatusAsync(string status) => dbContext.ExceptionRecords.CountAsync(x => x.Status == status);

    // ── Hubs ────────────────────────────────────────────────

    public async Task AddHubAsync(Hub hub) => await dbContext.Hubs.AddAsync(hub);

    public Task<List<Hub>> GetHubsAsync() => dbContext.Hubs
        .Include(x => x.ServiceLocations)
        .ToListAsync();

    public Task<List<Hub>> GetHubsPagedAsync(int pageNumber, int pageSize) => dbContext.Hubs
        .Include(x => x.ServiceLocations)
        .OrderBy(x => x.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    public Task<int> GetHubCountAsync() => dbContext.Hubs.CountAsync(x => x.IsActive);

    public Task<int> GetTotalHubCountAsync() => dbContext.Hubs.CountAsync();

    public Task<Hub?> GetHubAsync(Guid id) => dbContext.Hubs
        .Include(x => x.ServiceLocations)
        .FirstOrDefaultAsync(x => x.HubId == id);

    public Task DeleteHubAsync(Hub hub)
    {
        dbContext.Hubs.Remove(hub);
        return Task.CompletedTask;
    }

    // ── Locations ───────────────────────────────────────────

    public async Task AddLocationAsync(ServiceLocation location) => await dbContext.ServiceLocations.AddAsync(location);

    public Task<List<ServiceLocation>> GetLocationsByHubAsync(Guid hubId) => dbContext.ServiceLocations.Where(x => x.HubId == hubId).ToListAsync();

    public Task<List<ServiceLocation>> GetAllLocationsPagedAsync(int pageNumber, int pageSize) => dbContext.ServiceLocations
        .OrderBy(x => x.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    public Task<int> GetTotalLocationCountAsync() => dbContext.ServiceLocations.CountAsync();

    public Task<ServiceLocation?> GetLocationAsync(Guid id) => dbContext.ServiceLocations.FirstOrDefaultAsync(x => x.LocationId == id);

    public Task DeleteLocationAsync(ServiceLocation location)
    {
        dbContext.ServiceLocations.Remove(location);
        return Task.CompletedTask;
    }

    public Task<bool> HubExistsAsync(Guid hubId) => dbContext.Hubs.AnyAsync(x => x.HubId == hubId);

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}