using SmartShip.TrackingService.DTOs;

namespace SmartShip.TrackingService.Services;

public interface ITrackingService
{
    Task<object?> GetTrackingAsync(string trackingNumber);
    Task<List<object>> GetTrackingEventsAsync(string trackingNumber);
    Task<object?> GetTrackingStatusAsync(string trackingNumber);
    Task<(bool Ok, string? Message, object? Data)> UpdateTrackingStatusAsync(string trackingNumber, UpdateTrackingStatusDto request);
    Task<List<object>> GetTrackingTimelineAsync(string trackingNumber);
    Task<object?> CreateTrackingEventAsync(CreateTrackingEventDto request);
    Task<object?> UpdateTrackingEventAsync(Guid id, UpdateTrackingEventDto request);
    Task<bool> DeleteTrackingEventAsync(Guid id);
    Task<object?> CreateLocationUpdateAsync(CreateTrackingLocationDto request);
    Task<object?> GetCurrentLocationAsync(string trackingNumber);
}