using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.AdminService.DTOs;
using SmartShip.AdminService.Services;

namespace SmartShip.AdminService.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    // ── Logging Demo ────────────────────────────────────────

    [HttpGet("logging-demo")]
    public async Task<IActionResult> LoggingDemo([FromQuery] bool fail = false)
        => Ok(await adminService.LoggingDemoAsync(fail));

    // ── Dashboard & Statistics ──────────────────────────────

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
        => Ok(await adminService.GetDashboardAsync());

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
        => Ok(await adminService.GetStatisticsAsync());

    // ── Hubs ────────────────────────────────────────────────

    [HttpGet("hubs")]
    public async Task<IActionResult> GetHubs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        => Ok(await adminService.GetHubsPagedAsync(pageNumber, pageSize));

    [HttpPost("hubs")]
    public async Task<IActionResult> CreateHub([FromBody] CreateHubDto request)
        => Ok(await adminService.CreateHubAsync(request));

    [HttpGet("hubs/{id:guid}")]
    public async Task<IActionResult> GetHub(Guid id)
    {
        var hub = await adminService.GetHubAsync(id);
        return hub is null ? NotFound() : Ok(hub);
    }

    [HttpPut("hubs/{id:guid}")]
    public async Task<IActionResult> UpdateHub(Guid id, [FromBody] UpdateHubDto request)
    {
        var hub = await adminService.UpdateHubAsync(id, request);
        return hub is null ? NotFound() : Ok(hub);
    }

    [HttpDelete("hubs/{id:guid}")]
    public async Task<IActionResult> DeleteHub(Guid id)
    {
        var deleted = await adminService.DeleteHubAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ── Locations ───────────────────────────────────────────

    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        => Ok(await adminService.GetLocationsPagedAsync(pageNumber, pageSize));

    [HttpPost("locations")]
    public async Task<IActionResult> CreateLocation([FromBody] CreateServiceLocationDto request)
    {
        var location = await adminService.CreateLocationAsync(request);
        return location is null ? BadRequest(new { message = "Hub not found." }) : Ok(location);
    }

    [HttpPut("locations/{id:guid}")]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateServiceLocationDto request)
    {
        var location = await adminService.UpdateLocationAsync(id, request);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpDelete("locations/{id:guid}")]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        var deleted = await adminService.DeleteLocationAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ── Exceptions ──────────────────────────────────────────

    [HttpGet("exceptions")]
    public async Task<IActionResult> GetExceptions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        => Ok(await adminService.GetExceptionsPagedAsync(pageNumber, pageSize));

    [HttpGet("exceptions/shipment/{shipmentId:guid}")]
    public async Task<IActionResult> GetExceptionsByShipment(Guid shipmentId)
        => Ok(await adminService.GetExceptionsByShipmentAsync(shipmentId));

    [HttpPost("exceptions")]
    public async Task<IActionResult> CreateException([FromBody] CreateExceptionDto request)
        => Ok(await adminService.CreateExceptionRecordAsync(request));

    [HttpPut("exceptions/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveException(Guid id, [FromBody] ResolveExceptionDto request)
    {
        var result = await adminService.ResolveExceptionRecordAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Shipments ───────────────────────────────────────────

    [HttpPut("shipments/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveShipment(Guid id, [FromBody] ResolveShipmentDto request)
    {
        var result = await adminService.ResolveShipmentAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("shipments/{id:guid}/delay")]
    public async Task<IActionResult> DelayShipment(Guid id, [FromBody] DelayShipmentDto request)
    {
        var result = await adminService.DelayShipmentAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("shipments/{id:guid}/return")]
    public async Task<IActionResult> ReturnShipment(Guid id, [FromBody] ReturnShipmentDto request)
    {
        var result = await adminService.ReturnShipmentAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("shipments")]
    public async Task<IActionResult> GetShipments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        => Ok(await adminService.GetShipmentsPagedAsync(pageNumber, pageSize));

    [HttpGet("shipments/{id:guid}")]
    public async Task<IActionResult> GetShipment(Guid id)
    {
        var shipment = await adminService.GetShipmentAsync(id);
        return shipment is null ? NotFound() : Ok(shipment);
    }

    [HttpGet("shipments/hub/{hubId:guid}")]
    public async Task<IActionResult> GetShipmentsByHub(Guid hubId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        => Ok(await adminService.GetShipmentsByHubPagedAsync(hubId, pageNumber, pageSize));
}