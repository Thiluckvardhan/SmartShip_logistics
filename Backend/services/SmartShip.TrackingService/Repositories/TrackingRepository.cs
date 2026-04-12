using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Data;
using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.Repositories;

public class TrackingRepository(TrackingDbContext dbContext) : ITrackingRepository
{
    // ── Events ──────────────────────────────────────────────

    public async Task AddEventAsync(TrackingEvent trackingEvent) =>
        await dbContext.TrackingEvents.AddAsync(trackingEvent);

    public Task<TrackingEvent?> GetEventByIdAsync(Guid eventId) =>
        dbContext.TrackingEvents.FirstOrDefaultAsync(x => x.EventId == eventId);

    public Task DeleteEventAsync(TrackingEvent trackingEvent)
    {
        dbContext.TrackingEvents.Remove(trackingEvent);
        return Task.CompletedTask;
    }

    public Task<List<TrackingEvent>> GetEventsByTrackingNumberAsync(string trackingNumber) =>
        dbContext.TrackingEvents
            .Where(x => x.TrackingNumber == trackingNumber)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();

    public Task<TrackingEvent?> GetLatestEventByTrackingNumberAsync(string trackingNumber) =>
        dbContext.TrackingEvents
            .Where(x => x.TrackingNumber == trackingNumber)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

    // ── Locations ───────────────────────────────────────────

    public async Task AddLocationAsync(TrackingLocation location) =>
        await dbContext.TrackingLocations.AddAsync(location);

    public Task<TrackingLocation?> GetLatestLocationByTrackingNumberAsync(string trackingNumber) =>
        dbContext.TrackingLocations
            .Where(x => x.TrackingNumber == trackingNumber)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

    // ── Common ──────────────────────────────────────────────

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}