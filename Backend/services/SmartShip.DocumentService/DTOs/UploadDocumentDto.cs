using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartShip.DocumentService.DTOs;

public class UploadDocumentDto
{
    [Required(ErrorMessage = "Shipment ID is required.")]
    public Guid ShipmentId { get; set; }

    [Required(ErrorMessage = "File is required.")]
    public IFormFile File { get; set; } = default!;
}

public class UploadTypedDocumentDto
{
    [Required(ErrorMessage = "Shipment ID is required.")]
    public Guid ShipmentId { get; set; }

    [Required(ErrorMessage = "File is required.")]
    public IFormFile File { get; set; } = default!;
}

public class UpdateDocumentDto
{
    [Required(ErrorMessage = "Shipment ID is required.")]
    public Guid ShipmentId { get; set; }

    [Required(ErrorMessage = "File is required.")]
    public IFormFile File { get; set; } = default!;
}

public class CreateDeliveryProofDto
{
    [Required(ErrorMessage = "Signer name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Signer name must be between 2 and 200 characters.")]
    public string SignerName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "File is required.")]
    public IFormFile File { get; set; } = default!;
}
