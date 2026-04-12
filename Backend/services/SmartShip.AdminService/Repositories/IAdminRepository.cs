using SmartShip.AdminService.Models;

namespace SmartShip.AdminService.Repositories;

public interface IAdminRepository
{
    // Exceptions
    Task AddExceptionAsync(ExceptionRecord record);
    Task<List<ExceptionRecord>> GetExceptionsByShipmentAsync(Guid shipmentId);
    Task<List<ExceptionRecord>> GetAllExceptionsAsync();
    Task<List<ExceptionRecord>> GetExceptionsPagedAsync(int pageNumber, int pageSize);
    Task<int> GetExceptionCountAsync();
    Task<int> GetExceptionCountByStatusAsync(string status);
    Task<ExceptionRecord?> GetExceptionAsync(Guid id);
    Task<List<ExceptionRecord>> GetOpenExceptionsAsync();

    // Hubs
    Task AddHubAsync(Hub hub);
    Task<List<Hub>> GetHubsAsync();
    Task<List<Hub>> GetHubsPagedAsync(int pageNumber, int pageSize);
    Task<int> GetHubCountAsync();
    Task<int> GetTotalHubCountAsync();
    Task<Hub?> GetHubAsync(Guid id);
    Task DeleteHubAsync(Hub hub);

    // Locations
    Task AddLocationAsync(ServiceLocation location);
    Task<List<ServiceLocation>> GetLocationsByHubAsync(Guid hubId);
    Task<List<ServiceLocation>> GetAllLocationsPagedAsync(int pageNumber, int pageSize);
    Task<int> GetTotalLocationCountAsync();
    Task<ServiceLocation?> GetLocationAsync(Guid id);
    Task DeleteLocationAsync(ServiceLocation location);
    Task<bool> HubExistsAsync(Guid hubId);

    Task SaveChangesAsync();
}