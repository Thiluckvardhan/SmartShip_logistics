using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.Repositories;

public interface ITrackingRepository
{
    // Events
    Task AddEventAsync(TrackingEvent trackingEvent);
    Task<TrackingEvent?> GetEventByIdAsync(Guid eventId);
    Task DeleteEventAsync(TrackingEvent trackingEvent);
    Task<List<TrackingEvent>> GetEventsByTrackingNumberAsync(string trackingNumber);
    Task<TrackingEvent?> GetLatestEventByTrackingNumberAsync(string trackingNumber);

    // Locations
    Task AddLocationAsync(TrackingLocation location);
    Task<TrackingLocation?> GetLatestLocationByTrackingNumberAsync(string trackingNumber);

    Task SaveChangesAsync();
}