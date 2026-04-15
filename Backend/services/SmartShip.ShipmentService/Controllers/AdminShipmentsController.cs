using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;
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

    [HttpPut("/api/shipments/{id:guid}/admin-update-status")]
    public async Task<IActionResult> AdminUpdateStatus(Guid id, [FromBody] AdminUpdateShipmentJourneyDto request)
    {
        var result = await shipmentService.UpdateShipmentStatusWithJourneyAsync(id, request);
        return result.Ok
            ? Ok(result.Data)
            : result.Message == "Shipment not found."
                ? NotFound(new { message = result.Message })
                : BadRequest(new { message = result.Message });
    }

    [HttpPut("/api/shipments/{id:guid}/pickup")]
    [HttpPut("/shipment/api/shipments/{id:guid}/pickup")]
    public Task<IActionResult> MarkPickedUp(Guid id)
        => UpdateStatusByAction(id, ShipmentStatus.PickedUp);

    [HttpPut("/api/shipments/{id:guid}/in-transit")]
    [HttpPut("/shipment/api/shipments/{id:guid}/in-transit")]
    public Task<IActionResult> MarkInTransit(Guid id)
        => UpdateStatusByAction(id, ShipmentStatus.InTransit);

    [HttpPut("/api/shipments/{id:guid}/out-for-delivery")]
    [HttpPut("/shipment/api/shipments/{id:guid}/out-for-delivery")]
    public Task<IActionResult> MarkOutForDelivery(Guid id)
        => UpdateStatusByAction(id, ShipmentStatus.OutForDelivery);

    [HttpPut("/api/shipments/{id:guid}/delivered")]
    [HttpPut("/shipment/api/shipments/{id:guid}/delivered")]
    public Task<IActionResult> MarkDelivered(Guid id)
        => UpdateStatusByAction(id, ShipmentStatus.Delivered);

    [HttpPut("/api/shipments/{id:guid}/delay")]
    [HttpPut("/shipment/api/shipments/{id:guid}/delay")]
    public Task<IActionResult> MarkDelayed(Guid id)
        => UpdateStatusByAction(id, ShipmentStatus.Delayed);

    [HttpPut("/api/shipments/{id:guid}/return")]
    [HttpPut("/shipment/api/shipments/{id:guid}/return")]
    public Task<IActionResult> MarkReturned(Guid id)
        => UpdateStatusByAction(id, ShipmentStatus.Returned);

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

    private async Task<IActionResult> UpdateStatusByAction(Guid id, ShipmentStatus status)
    {
        var result = await shipmentService.UpdateShipmentStatusAsync(id, status);
        return result.Ok
            ? Ok(result.Data)
            : result.Message == "Shipment not found."
                ? NotFound(new { message = result.Message })
                : BadRequest(new { message = result.Message });
    }
}
