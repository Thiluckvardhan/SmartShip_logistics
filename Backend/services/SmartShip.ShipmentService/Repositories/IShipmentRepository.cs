using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Repositories;

public interface IShipmentRepository
{
    Task AddAddressAsync(Address address);
    Task<Address?> GetAddressAsync(Guid id);
    Task DeleteAddressAsync(Address address);
    Task<bool> IsAddressUsedByOtherShipmentsAsync(Guid addressId, Guid excludingShipmentId);
    Task AddShipmentAsync(Shipment shipment);
    Task<Shipment?> GetShipmentAsync(Guid id);
    Task<Shipment?> GetShipmentByTrackingNumberAsync(string trackingNumber);
    Task<List<Shipment>> GetShipmentsByCustomerAsync(Guid customerId);
    Task<List<Shipment>> GetAllShipmentsAsync();
    Task<int> GetShipmentCountAsync();
    Task<int> GetShipmentCountByStatusAsync(ShipmentStatus status);
    Task DeleteShipmentAsync(Shipment shipment);
    Task AddPackageAsync(Package package);
    Task<Package?> GetPackageAsync(Guid shipmentId, Guid packageId);
    Task DeletePackageAsync(Package package);
    Task<List<Package>> GetPackagesByShipmentAsync(Guid shipmentId);
    Task AddPickupAsync(PickupSchedule pickup);
    Task<List<PickupSchedule>> GetAllPickupsAsync();
    Task<List<PickupSchedule>> GetPickupsByShipmentAsync(Guid shipmentId);
    Task<PickupSchedule?> GetLatestPickupByShipmentAsync(Guid shipmentId);
    Task AddOutboxMessageAsync(OutboxMessage outboxMessage);
    Task<List<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchSize);
    Task SaveChangesAsync();
}