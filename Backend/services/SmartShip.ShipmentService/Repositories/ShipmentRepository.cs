using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Repositories;

public class ShipmentRepository(ShipmentDbContext dbContext) : IShipmentRepository
{
    public async Task AddAddressAsync(Address address) => await dbContext.Addresses.AddAsync(address);

    public Task<Address?> GetAddressAsync(Guid id) => dbContext.Addresses.FirstOrDefaultAsync(x => x.AddressId == id);

    public Task DeleteAddressAsync(Address address)
    {
        dbContext.Addresses.Remove(address);
        return Task.CompletedTask;
    }

    public Task<bool> IsAddressUsedByOtherShipmentsAsync(Guid addressId, Guid excludingShipmentId) =>
        dbContext.Shipments.AnyAsync(x =>
            x.ShipmentId != excludingShipmentId &&
            (x.SenderAddressId == addressId || x.ReceiverAddressId == addressId));

    public async Task AddShipmentAsync(Shipment shipment) => await dbContext.Shipments.AddAsync(shipment);

    public Task<Shipment?> GetShipmentAsync(Guid id) => dbContext.Shipments
        .Include(x => x.SenderAddress)
        .Include(x => x.ReceiverAddress)
        .Include(x => x.Packages)
        .Include(x => x.PickupSchedules)
        .FirstOrDefaultAsync(x => x.ShipmentId == id);

    public Task<Shipment?> GetShipmentByTrackingNumberAsync(string trackingNumber) => dbContext.Shipments
        .Include(x => x.SenderAddress)
        .Include(x => x.ReceiverAddress)
        .Include(x => x.Packages)
        .Include(x => x.PickupSchedules)
        .FirstOrDefaultAsync(x => x.TrackingNumber == trackingNumber);

    public Task<List<Shipment>> GetShipmentsByCustomerAsync(Guid customerId) => dbContext.Shipments
        .Include(x => x.SenderAddress)
        .Include(x => x.ReceiverAddress)
        .Include(x => x.Packages)
        .Where(x => x.CustomerId == customerId)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<List<Shipment>> GetAllShipmentsAsync() => dbContext.Shipments
        .Include(x => x.SenderAddress)
        .Include(x => x.ReceiverAddress)
        .Include(x => x.Packages)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<int> GetShipmentCountAsync() => dbContext.Shipments.CountAsync();

    public Task<int> GetShipmentCountByStatusAsync(ShipmentStatus status) => dbContext.Shipments.CountAsync(x => x.Status == status);

    public Task DeleteShipmentAsync(Shipment shipment)
    {
        dbContext.Shipments.Remove(shipment);
        return Task.CompletedTask;
    }

    public async Task AddPackageAsync(Package package) => await dbContext.Packages.AddAsync(package);

    public Task<Package?> GetPackageAsync(Guid shipmentId, Guid packageId) =>
        dbContext.Packages.FirstOrDefaultAsync(x => x.ShipmentId == shipmentId && x.PackageId == packageId);

    public Task DeletePackageAsync(Package package)
    {
        dbContext.Packages.Remove(package);
        return Task.CompletedTask;
    }

    public Task<List<Package>> GetPackagesByShipmentAsync(Guid shipmentId) => dbContext.Packages.Where(x => x.ShipmentId == shipmentId).ToListAsync();

    public async Task AddPickupAsync(PickupSchedule pickup) => await dbContext.PickupSchedules.AddAsync(pickup);

    public Task<List<PickupSchedule>> GetAllPickupsAsync() => dbContext.PickupSchedules
        .OrderByDescending(x => x.PickupDate)
        .ToListAsync();

    public Task<List<PickupSchedule>> GetPickupsByShipmentAsync(Guid shipmentId) => dbContext.PickupSchedules.Where(x => x.ShipmentId == shipmentId).ToListAsync();

    public Task<PickupSchedule?> GetLatestPickupByShipmentAsync(Guid shipmentId) => dbContext.PickupSchedules
        .OrderByDescending(x => x.PickupDate)
        .FirstOrDefaultAsync(x => x.ShipmentId == shipmentId);

    public async Task AddOutboxMessageAsync(OutboxMessage outboxMessage) =>
        await dbContext.OutboxMessages.AddAsync(outboxMessage);

    public Task<List<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchSize) =>
        dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.Status != "Published")
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Take(batchSize)
            .ToListAsync();

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}