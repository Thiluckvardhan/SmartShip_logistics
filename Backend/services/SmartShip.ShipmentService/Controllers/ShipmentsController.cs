using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Services;

namespace SmartShip.ShipmentService.Controllers;

[ApiController]
[Authorize]
public class ShipmentsController(IShipmentService shipmentService) : ControllerBase
{
    [HttpPost("/api/shipments")]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var created = await shipmentService.CreateShipmentAsync(request, userId);
        return Created("/api/shipments", created);
    }

    [HttpGet("/api/shipments/{id:guid}")]
    public async Task<IActionResult> GetShipment(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var item = await shipmentService.GetShipmentAsync(id, userId, User.IsInRole("Admin"));
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("/api/shipments")]
    public async Task<IActionResult> GetShipments()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        return Ok(await shipmentService.GetMyShipmentsAsync(userId));
    }

    [HttpGet("/api/shipments/track/{trackingNumber}")]
    public async Task<IActionResult> GetByTrackingNumber(string trackingNumber)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var item = await shipmentService.GetShipmentByTrackingNumberAsync(trackingNumber, userId, User.IsInRole("Admin"));
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("/api/shipments/{id:guid}")]
    public async Task<IActionResult> UpdateShipment(Guid id, [FromBody] UpdateShipmentDto request)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "At least one package item is required." });

        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var updated = await shipmentService.UpdateShipmentAsync(id, request, userId, User.IsInRole("Admin"));
        return updated is null
            ? NotFound(new { message = "Shipment not found." })
            : Ok(updated);
    }

    [HttpPost("/api/shipments/{id:guid}/book")]
    public async Task<IActionResult> BookShipment(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await shipmentService.BookShipmentAsync(id, userId);
        if (result.Ok) return Ok(result.Data);

        return result.Message switch
        {
            "Shipment not found." => NotFound(new { message = result.Message }),
            "You can book only your own shipments." => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpPost("/api/shipments/calculate-rate")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculateRate([FromBody] CalculateRateDto request)
    {
        return Ok(await shipmentService.CalculateRateAsync(request));
    }

    [HttpDelete("/api/shipments/{id:guid}")]
    public async Task<IActionResult> DeleteShipment(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await shipmentService.DeleteShipmentAsync(id, userId, User.IsInRole("Admin"));
        if (result.Ok)
            return Ok(new { message = "Shipment deleted successfully.", shipmentId = id });

        return result.Message switch
        {
            "Shipment not found." => NotFound(new { message = result.Message }),
            "You can delete only your own shipments." => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            "Cannot delete shipment now because the shipment is beyond Booked." => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpPost("/api/addresses")]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto request) => Ok(await shipmentService.CreateAddressAsync(request));

    [HttpGet("/api/addresses/{id:guid}")]
    public async Task<IActionResult> GetAddress(Guid id)
    {
        var item = await shipmentService.GetAddressAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("/api/shipments/{id:guid}/packages")]
    public async Task<IActionResult> CreatePackage(Guid id, [FromBody] CreatePackageDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var item = await shipmentService.CreatePackageAsync(id, request, userId, User.IsInRole("Admin"));
        return item is null ? BadRequest(new { message = "Shipment not found." }) : Ok(item);
    }

    [HttpGet("/api/shipments/{id:guid}/packages")]
    public async Task<IActionResult> GetPackages(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var items = await shipmentService.GetPackagesByShipmentAsync(id, userId, User.IsInRole("Admin"));
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPut("/api/shipments/{id:guid}/packages/{packageId:guid}")]
    public async Task<IActionResult> UpdatePackage(Guid id, Guid packageId, [FromBody] UpdatePackageDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var item = await shipmentService.UpdatePackageAsync(id, packageId, request, userId, User.IsInRole("Admin"));
        return item is null ? NotFound(new { message = "Shipment or package not found." }) : Ok(item);
    }

    [HttpDelete("/api/shipments/{id:guid}/packages/{packageId:guid}")]
    public async Task<IActionResult> DeletePackage(Guid id, Guid packageId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await shipmentService.DeletePackageAsync(id, packageId, userId, User.IsInRole("Admin"));
        if (result.Ok) return Ok(new { message = "Package deleted successfully.", shipmentId = id, packageId });

        return result.Message switch
        {
            "Shipment not found." => NotFound(new { message = result.Message }),
            "Package not found." => NotFound(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }
}