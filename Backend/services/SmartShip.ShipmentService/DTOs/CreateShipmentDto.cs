using System.ComponentModel.DataAnnotations;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.DTOs;

public record AddressDto(
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    string Name,

    [Required(ErrorMessage = "Phone is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters.")]
    string Phone,

    [Required(ErrorMessage = "Street is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Street must be between 2 and 200 characters.")]
    string Street,

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
    string City,

    [Required(ErrorMessage = "State is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters.")]
    string State,

    [Required(ErrorMessage = "Country is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters.")]
    string Country,

    [Required(ErrorMessage = "Pincode is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Pincode must be between 3 and 20 characters.")]
    string Pincode);

public record ShipmentItemDto(
    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 200 characters.")]
    string ItemName,

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    int Quantity,

    [Required(ErrorMessage = "Weight is required.")]
    [Range(0.01, 50000, ErrorMessage = "Weight must be between 0.01 and 50,000 kg.")]
    decimal Weight,

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    string? Description);

public record CreateShipmentDto(
    [Required(ErrorMessage = "Sender address is required.")]
    AddressDto SenderAddress,

    [Required(ErrorMessage = "Receiver address is required.")]
    AddressDto ReceiverAddress,

    [Required(ErrorMessage = "At least one item is required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    List<ShipmentItemDto> Items);

public record UpdateShipmentDto(
    [Required(ErrorMessage = "At least one item is required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    List<ShipmentItemDto> Items);

public record UpdateShipmentStatusDto(
    [Required(ErrorMessage = "Status is required.")]
    ShipmentStatus Status);

public record CalculateRateDto(
    [Required(ErrorMessage = "Total weight is required.")]
    [Range(0.01, 50000, ErrorMessage = "Total weight must be between 0.01 and 50,000 kg.")]
    decimal TotalWeight);

public record CreateAddressDto(
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    string Name,

    [Required(ErrorMessage = "Phone is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters.")]
    string Phone,

    [Required(ErrorMessage = "Street is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Street must be between 2 and 200 characters.")]
    string Street,

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
    string City,

    [Required(ErrorMessage = "State is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters.")]
    string State,

    [Required(ErrorMessage = "Country is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters.")]
    string Country,

    [Required(ErrorMessage = "Pincode is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Pincode must be between 3 and 20 characters.")]
    string Pincode);

public record CreatePackageDto(
    [Required(ErrorMessage = "Weight is required.")]
    [Range(0.01, 50000, ErrorMessage = "Weight must be between 0.01 and 50,000 kg.")]
    decimal Weight,

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    string? Description,

    [StringLength(200, ErrorMessage = "Item name must not exceed 200 characters.")]
    string? ItemName,

    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    int? Quantity);

public record UpdatePackageDto(
    [Range(0.01, 50000, ErrorMessage = "Weight must be between 0.01 and 50,000 kg.")]
    decimal? Weight,

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    string? Description,

    [StringLength(200, ErrorMessage = "Item name must not exceed 200 characters.")]
    string? ItemName,

    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    int? Quantity);

public record CreatePickupDto(
    [Required(ErrorMessage = "Shipment ID is required.")]
    Guid ShipmentId,

    [Required(ErrorMessage = "Pickup date is required.")]
    DateTime PickupDate,

    [StringLength(500, ErrorMessage = "Notes must not exceed 500 characters.")]
    string? Notes);

public record UpdatePickupDto(
    DateTime? PickupDate,

    [StringLength(500, ErrorMessage = "Notes must not exceed 500 characters.")]
    string? Notes);