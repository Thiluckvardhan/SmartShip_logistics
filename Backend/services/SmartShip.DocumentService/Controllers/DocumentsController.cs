using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.DocumentService.DTOs;
using SmartShip.DocumentService.Services;

namespace SmartShip.DocumentService.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentsController(IDocumentService documentService, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg"
    };

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto request)
    {
        var customerId = GetUserId();
        if (customerId == Guid.Empty) return Unauthorized();
        if (request.File is null) return BadRequest(new { message = "File is required." });

        var ext = Path.GetExtension(request.File.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"File type '{ext}' is not allowed. Allowed: PDF, PNG, JPG, JPEG." });

        if (request.File.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds maximum of 10MB." });

        var result = await documentService.UploadDocumentAsync(request.ShipmentId, "General", request.File, customerId, environment.ContentRootPath);
        return result is null ? BadRequest(new { message = "Failed to upload document." }) : Ok(result);
    }

    [HttpPost("upload-invoice")]
    public async Task<IActionResult> UploadInvoiceDocument([FromForm] UploadTypedDocumentDto request)
        => await UploadTypedDocument(request, "Invoice");

    [HttpPost("upload-label")]
    public async Task<IActionResult> UploadLabelDocument([FromForm] UploadTypedDocumentDto request)
        => await UploadTypedDocument(request, "Label");

    [HttpPost("upload-customs")]
    public async Task<IActionResult> UploadCustomsDocument([FromForm] UploadTypedDocumentDto request)
        => await UploadTypedDocument(request, "Customs");

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var item = await documentService.GetDocumentAsync(id);
        return item is null ? NotFound(new { message = "Document not found." }) : Ok(item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromForm] UpdateDocumentDto request)
    {
        if (request.File is not null)
        {
            var ext = Path.GetExtension(request.File.FileName);
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = $"File type '{ext}' is not allowed. Allowed: PDF, PNG, JPG, JPEG." });

            if (request.File.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds maximum of 10MB." });
        }

        var updated = await documentService.UpdateDocumentAsync(id, request.ShipmentId, request.File, environment.ContentRootPath);
        return updated is null ? NotFound(new { message = "Document not found." }) : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var deleted = await documentService.DeleteDocumentAsync(id);
        return deleted ? Ok(new { message = "Document deleted successfully.", id }) : NotFound(new { message = "Document not found." });
    }

    [HttpGet("shipment/{shipmentId:guid}")]
    public async Task<IActionResult> GetDocumentsByShipment(Guid shipmentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        => Ok(await documentService.GetDocumentsAsync(shipmentId, pageNumber, pageSize));

    [HttpGet("customer/{customerId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDocumentsByCustomer(Guid customerId)
        => Ok(await documentService.GetDocumentsByCustomerAsync(customerId));

    [HttpPost("delivery-proof/{shipmentId:guid}")]
    public async Task<IActionResult> CreateDeliveryProof(Guid shipmentId, [FromForm] CreateDeliveryProofDto request)
    {
        if (request.File is null) return BadRequest(new { message = "Proof file is required." });

        if (request.File.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds maximum of 10MB." });

        var result = await documentService.CreateDeliveryProofAsync(shipmentId, request.SignerName, request.Notes, request.File, environment.ContentRootPath);
        return result is null ? BadRequest(new { message = "Failed to upload proof." }) : Ok(result);
    }

    [HttpGet("delivery-proof/{shipmentId:guid}")]
    public async Task<IActionResult> GetDeliveryProof(Guid shipmentId) => Ok(await documentService.GetDeliveryProofAsync(shipmentId));

    private async Task<IActionResult> UploadTypedDocument(UploadTypedDocumentDto request, string documentType)
    {
        var customerId = GetUserId();
        if (customerId == Guid.Empty) return Unauthorized();
        if (request.File is null) return BadRequest(new { message = "File is required." });

        var ext = Path.GetExtension(request.File.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"File type '{ext}' is not allowed. Allowed: PDF, PNG, JPG, JPEG." });

        if (request.File.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds maximum of 10MB." });

        var result = await documentService.UploadDocumentAsync(request.ShipmentId, documentType, request.File, customerId, environment.ContentRootPath);
        return result is null ? BadRequest(new { message = "Failed to upload document." }) : Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }
}