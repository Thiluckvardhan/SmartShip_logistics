using SmartShip.TrackingService.DTOs;
using SmartShip.TrackingService.Models;
using SmartShip.TrackingService.Repositories;

namespace SmartShip.TrackingService.Services;

public class TrackingService(ITrackingRepository repository) : ITrackingService
{
    public async Task<object?> GetTrackingAsync(string trackingNumber)
    {
        var latest = await repository.GetLatestEventByTrackingNumberAsync(trackingNumber.Trim());
        if (latest is null) return null;

        return new
        {
            latest.TrackingNumber,
            CurrentStatus = latest.Status,
            CurrentLocation = latest.Location,
            LastUpdatedAt = latest.Timestamp
        };
    }

    public async Task<List<object>> GetTrackingEventsAsync(string trackingNumber)
        => (await repository.GetEventsByTrackingNumberAsync(trackingNumber.Trim()))
            .OrderByDescending(x => x.Timestamp)
            .Select(MapEvent)
            .Cast<object>()
            .ToList();

    public async Task<object?> GetTrackingStatusAsync(string trackingNumber)
    {
        var latest = await repository.GetLatestEventByTrackingNumberAsync(trackingNumber.Trim());
        if (latest is null) return null;

        return new
        {
            latest.TrackingNumber,
            latest.Status,
            latest.Location,
            latest.Timestamp
        };
    }

    public async Task<(bool Ok, string? Message, object? Data)> UpdateTrackingStatusAsync(string trackingNumber, UpdateTrackingStatusDto request)
    {
        trackingNumber = trackingNumber.Trim();
        var latest = await repository.GetLatestEventByTrackingNumberAsync(trackingNumber);
        if (latest is null) return (false, "Tracking number not found.", null);

        var entity = new TrackingEvent
        {
            EventId = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Status = request.Status.Trim(),
            Location = request.Location?.Trim() ?? latest.Location,
            Description = request.Description?.Trim() ?? $"Status changed to {request.Status.Trim()}",
            Timestamp = DateTime.UtcNow
        };

        await repository.AddEventAsync(entity);
        await repository.SaveChangesAsync();
        return (true, null, MapEvent(entity));
    }

    public async Task<List<object>> GetTrackingTimelineAsync(string trackingNumber)
        => (await repository.GetEventsByTrackingNumberAsync(trackingNumber.Trim()))
            .OrderBy(x => x.Timestamp)
            .Select(MapEvent)
            .Cast<object>()
            .ToList();

    public async Task<object?> CreateTrackingEventAsync(CreateTrackingEventDto request)
    {
        var entity = new TrackingEvent
        {
            EventId = Guid.NewGuid(),
            TrackingNumber = request.TrackingNumber.Trim(),
            Status = request.Status.Trim(),
            Location = request.Location.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Timestamp = request.Timestamp
        };

        await repository.AddEventAsync(entity);
        await repository.SaveChangesAsync();
        return MapEvent(entity);
    }

    public async Task<object?> UpdateTrackingEventAsync(Guid id, UpdateTrackingEventDto request)
    {
        var tracking = await repository.GetEventByIdAsync(id);
        if (tracking is null) return null;

        tracking.TrackingNumber = request.TrackingNumber.Trim();
        tracking.Status = request.Status.Trim();
        tracking.Location = request.Location.Trim();
        tracking.Description = request.Description?.Trim() ?? tracking.Description;
        tracking.Timestamp = request.Timestamp;

        await repository.SaveChangesAsync();
        return MapEvent(tracking);
    }

    public async Task<bool> DeleteTrackingEventAsync(Guid id)
    {
        var tracking = await repository.GetEventByIdAsync(id);
        if (tracking is null) return false;

        await repository.DeleteEventAsync(tracking);
        await repository.SaveChangesAsync();
        return true;
    }

    public async Task<object?> CreateLocationUpdateAsync(CreateTrackingLocationDto request)
    {
        var trackingNumber = request.TrackingNumber.Trim();
        var latest = await repository.GetLatestEventByTrackingNumberAsync(trackingNumber);
        if (latest is null) return null;

        var entity = new TrackingLocation
        {
            LocationId = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Timestamp = request.Timestamp
        };

        await repository.AddLocationAsync(entity);
        await repository.SaveChangesAsync();
        return new
        {
            entity.LocationId,
            entity.TrackingNumber,
            entity.Latitude,
            entity.Longitude,
            entity.Timestamp
        };
    }

    public async Task<object?> GetCurrentLocationAsync(string trackingNumber)
    {
        var latest = await repository.GetLatestLocationByTrackingNumberAsync(trackingNumber.Trim());
        if (latest is null) return null;

        return new
        {
            latest.TrackingNumber,
            latest.Latitude,
            latest.Longitude,
            latest.Timestamp
        };
    }

    private static object MapEvent(TrackingEvent e) => new
    {
        e.EventId,
        e.TrackingNumber,
        e.Status,
        e.Location,
        e.Description,
        e.Timestamp
    };
}