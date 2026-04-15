using System.ComponentModel.DataAnnotations;

namespace SmartShip.AdminService.DTOs;

// ── Hub DTOs ────────────────────────────────────────────────
public record CreateHubDto(
    [Required(ErrorMessage = "Hub name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    string Name,

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Address must be between 2 and 500 characters.")]
    string Address,

    [Required(ErrorMessage = "Contact number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(30, ErrorMessage = "Contact number must not exceed 30 characters.")]
    string ContactNumber,

    [Required(ErrorMessage = "Manager name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Manager name must be between 2 and 200 characters.")]
    string ManagerName,

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    string Email,

    [Required(ErrorMessage = "IsActive is required.")]
    bool IsActive);

public record UpdateHubDto(
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    string? Name,

    [StringLength(500, MinimumLength = 2, ErrorMessage = "Address must be between 2 and 500 characters.")]
    string? Address,

    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(30, ErrorMessage = "Contact number must not exceed 30 characters.")]
    string? ContactNumber,

    [StringLength(200, MinimumLength = 2, ErrorMessage = "Manager name must be between 2 and 200 characters.")]
    string? ManagerName,

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    string? Email,

    bool? IsActive);

// ── Location DTOs ───────────────────────────────────────────
public record CreateServiceLocationDto(
    [Required(ErrorMessage = "Hub ID is required.")]
    Guid HubId,

    [Required(ErrorMessage = "Location name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    string Name,

    [Required(ErrorMessage = "Zip code is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Zip code must be between 3 and 20 characters.")]
    string ZipCode,

    [Required(ErrorMessage = "IsActive is required.")]
    bool IsActive);

public record UpdateServiceLocationDto(
    [Required(ErrorMessage = "Hub ID is required.")]
    Guid HubId,

    [Required(ErrorMessage = "Location name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    string Name,

    [Required(ErrorMessage = "Zip code is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Zip code must be between 3 and 20 characters.")]
    string ZipCode,

    [Required(ErrorMessage = "IsActive is required.")]
    bool IsActive);

// ── Exception DTOs ──────────────────────────────────────────
public record CreateExceptionDto(
    [Required(ErrorMessage = "Shipment ID is required.")]
    Guid ShipmentId,

    [Required(ErrorMessage = "Exception type is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Exception type must be between 2 and 100 characters.")]
    string ExceptionType,

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 1000 characters.")]
    string Description,

    [StringLength(50, ErrorMessage = "Status must not exceed 50 characters.")]
    string? Status,

    DateTime? ResolvedAt);

public record UpdateExceptionDto(
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Exception type must be between 2 and 100 characters.")]
    string? ExceptionType,

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    string? Description,

    [StringLength(50, ErrorMessage = "Status must not exceed 50 characters.")]
    string? Status,

    DateTime? ResolvedAt);

public record ResolveExceptionDto(
    [Required(ErrorMessage = "Resolution description is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Resolution description must be between 5 and 1000 characters.")]
    string Description);

// ── Shipment Action DTOs ────────────────────────────────────
public record ResolveShipmentDto(
    [Required(ErrorMessage = "Resolution notes are required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Resolution notes must be between 5 and 1000 characters.")]
    string ResolutionNotes);

public record DelayShipmentDto(
    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 1000 characters.")]
    string Reason);

public record ReturnShipmentDto(
    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 1000 characters.")]
    string Reason);