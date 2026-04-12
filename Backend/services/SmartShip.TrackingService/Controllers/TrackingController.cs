using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.TrackingService.DTOs;
using SmartShip.TrackingService.Services;

namespace SmartShip.TrackingService.Controllers;

[ApiController]
[Authorize]
[Route("api/tracking")]
public class TrackingController(ITrackingService trackingService) : ControllerBase
{
    [HttpGet("{trackingNumber}")]
    public async Task<IActionResult> GetByTrackingNumber(string trackingNumber)
    {
        var tracking = await trackingService.GetTrackingAsync(trackingNumber);
        return tracking is null ? NotFound(new { message = "Tracking number not found." }) : Ok(tracking);
    }

    [HttpGet("{trackingNumber}/timeline")]
    public async Task<IActionResult> GetTimeline(string trackingNumber)
    {
        return Ok(await trackingService.GetTrackingTimelineAsync(trackingNumber));
    }

    [HttpGet("{trackingNumber}/events")]
    public async Task<IActionResult> GetEvents(string trackingNumber)
    {
        return Ok(await trackingService.GetTrackingEventsAsync(trackingNumber));
    }

    [HttpPost("events")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateTrackingEventDto request)
    {
        var created = await trackingService.CreateTrackingEventAsync(request);
        return Ok(created);
    }

    [HttpPut("events/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateTrackingEventDto request)
    {
        var updated = await trackingService.UpdateTrackingEventAsync(id, request);
        return updated is null ? NotFound(new { message = "Tracking event not found." }) : Ok(updated);
    }

    [HttpDelete("events/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var deleted = await trackingService.DeleteTrackingEventAsync(id);
        return deleted ? Ok(new { message = "Tracking event deleted successfully.", id }) : NotFound(new { message = "Tracking event not found." });
    }

    [HttpPost("location")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLocationUpdate([FromBody] CreateTrackingLocationDto request)
    {
        var created = await trackingService.CreateLocationUpdateAsync(request);
        return created is null ? NotFound(new { message = "Tracking number not found." }) : Ok(created);
    }

    [HttpGet("location/{trackingNumber}")]
    public async Task<IActionResult> GetCurrentLocation(string trackingNumber)
    {
        var location = await trackingService.GetCurrentLocationAsync(trackingNumber);
        return location is null ? NotFound(new { message = "Tracking number not found." }) : Ok(location);
    }

    [HttpGet("{trackingNumber}/status")]
    public async Task<IActionResult> GetStatus(string trackingNumber)
    {
        var status = await trackingService.GetTrackingStatusAsync(trackingNumber);
        return status is null ? NotFound(new { message = "Tracking number not found." }) : Ok(status);
    }

    [HttpPut("{trackingNumber}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(string trackingNumber, [FromBody] UpdateTrackingStatusDto request)
    {
        var result = await trackingService.UpdateTrackingStatusAsync(trackingNumber, request);
        return result.Ok
            ? Ok(result.Data)
            : result.Message == "Tracking number not found."
                ? NotFound(new { message = result.Message })
                : BadRequest(new { message = result.Message });
    }
}