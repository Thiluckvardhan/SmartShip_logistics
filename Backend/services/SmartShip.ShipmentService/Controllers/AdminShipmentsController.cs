using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Services;

namespace SmartShip.ShipmentService.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public class AdminShipmentsController(IShipmentService shipmentService) : ControllerBase
{
    [HttpGet("/api/shipments/all")]
    public async Task<IActionResult> GetAllShipments()
    {
        return Ok(await shipmentService.GetAllShipmentsAsync());
    }

    [HttpGet("/api/shipments/stats")]
    public async Task<IActionResult> GetStats()
    {
        return Ok(await shipmentService.GetShipmentStatsAsync());
    }

    [HttpPut("/api/shipments/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShipmentStatusDto request)
    {
        var result = await shipmentService.UpdateShipmentStatusAsync(id, request.Status);
        return result.Ok
            ? Ok(result.Data)
            : result.Message == "Shipment not found."
                ? NotFound(new { message = result.Message })
                : BadRequest(new { message = result.Message });
    }

    [HttpPost("/api/pickups")]
    public async Task<IActionResult> CreatePickup([FromBody] CreatePickupDto request)
    {
        var item = await shipmentService.CreatePickupAsync(request);
        return item is null ? BadRequest(new { message = "Shipment not found." }) : Ok(item);
    }

    [HttpGet("/api/pickups")]
    public async Task<IActionResult> GetAllPickups() => Ok(await shipmentService.GetAllPickupsAsync());

    [HttpGet("/api/pickups/{shipmentId:guid}")]
    public async Task<IActionResult> GetPickups(Guid shipmentId) => Ok(await shipmentService.GetPickupsAsync(shipmentId));

    [HttpPut("/api/pickups/{shipmentId:guid}")]
    public async Task<IActionResult> UpdatePickup(Guid shipmentId, [FromBody] UpdatePickupDto request)
    {
        var item = await shipmentService.UpdatePickupAsync(shipmentId, request);
        return item is null ? NotFound(new { message = "Pickup schedule not found." }) : Ok(item);
    }
}
