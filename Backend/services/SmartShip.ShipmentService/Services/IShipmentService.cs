using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Services;

public interface IShipmentService
{
    Task<object?> CreateShipmentAsync(CreateShipmentDto request, Guid customerId);
    Task<object?> GetShipmentAsync(Guid id);
    Task<object?> GetShipmentByTrackingNumberAsync(string trackingNumber);
    Task<List<object>> GetMyShipmentsAsync(Guid customerId);
    Task<List<object>> GetAllShipmentsAsync();
    Task<(bool Ok, string? Message, object? Data)> BookShipmentAsync(Guid id, Guid customerId);
    Task<object?> UpdateShipmentAsync(Guid id, UpdateShipmentDto request);
    Task<(bool Ok, string? Message, object? Data)> UpdateShipmentStatusAsync(Guid id, ShipmentStatus newStatus);
    Task<(bool Ok, string? Message)> DeleteShipmentAsync(Guid id, Guid requesterId, bool isAdmin);
    Task<object> CalculateRateAsync(CalculateRateDto request);
    Task<object> CreateAddressAsync(CreateAddressDto request);
    Task<object?> GetAddressAsync(Guid id);
    Task<object?> CreatePackageAsync(Guid shipmentId, CreatePackageDto request);
    Task<object?> UpdatePackageAsync(Guid shipmentId, Guid packageId, UpdatePackageDto request);
    Task<(bool Ok, string? Message)> DeletePackageAsync(Guid shipmentId, Guid packageId);
    Task<List<object>> GetPackagesByShipmentAsync(Guid shipmentId);
    Task<object?> CreatePickupAsync(CreatePickupDto request);
    Task<List<object>> GetAllPickupsAsync();
    Task<List<object>> GetPickupsAsync(Guid shipmentId);
    Task<object?> UpdatePickupAsync(Guid shipmentId, UpdatePickupDto request);
    Task<object> GetShipmentStatsAsync();
}