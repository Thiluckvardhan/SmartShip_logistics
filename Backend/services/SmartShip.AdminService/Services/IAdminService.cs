using SmartShip.AdminService.DTOs;

namespace SmartShip.AdminService.Services;

public interface IAdminService
{
    // Dashboard & Statistics
    Task<object> GetDashboardAsync();
    Task<object> GetStatisticsAsync();
    Task<object> LoggingDemoAsync(bool fail);

    // Hubs
    Task<object> CreateHubAsync(CreateHubDto request);
    Task<object> GetHubsPagedAsync(int pageNumber, int pageSize);
    Task<object?> GetHubAsync(Guid id);
    Task<object?> UpdateHubAsync(Guid id, UpdateHubDto request);
    Task<bool> DeleteHubAsync(Guid id);

    // Locations
    Task<object> GetLocationsPagedAsync(int pageNumber, int pageSize);
    Task<object?> CreateLocationAsync(CreateServiceLocationDto request);
    Task<object?> UpdateLocationAsync(Guid id, UpdateServiceLocationDto request);
    Task<bool> DeleteLocationAsync(Guid id);

    // Exceptions
    Task<object> GetExceptionsPagedAsync(int pageNumber, int pageSize);

    // Shipments
    Task<object?> ResolveShipmentAsync(Guid id, ResolveShipmentDto request);
    Task<object?> DelayShipmentAsync(Guid id, DelayShipmentDto request);
    Task<object?> ReturnShipmentAsync(Guid id, ReturnShipmentDto request);
    Task<object> GetShipmentsPagedAsync(int pageNumber, int pageSize);
    Task<object?> GetShipmentAsync(Guid id);
    Task<object> GetShipmentsByHubPagedAsync(Guid hubId, int pageNumber, int pageSize);
}