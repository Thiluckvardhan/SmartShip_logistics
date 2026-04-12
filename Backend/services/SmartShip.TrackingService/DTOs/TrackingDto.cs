using System.ComponentModel.DataAnnotations;

namespace SmartShip.TrackingService.DTOs;

public record CreateTrackingEventDto(
    int EventId,

    [Required(ErrorMessage = "Tracking number is required.")]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "Tracking number must be between 1 and 32 characters.")]
    string TrackingNumber,

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Status must be between 1 and 50 characters.")]
    string Status,

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Location must be between 1 and 200 characters.")]
    string Location,

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    string? Description,

    [Required(ErrorMessage = "Timestamp is required.")]
    DateTime Timestamp);

public record UpdateTrackingEventDto(
    int EventId,

    [Required(ErrorMessage = "Tracking number is required.")]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "Tracking number must be between 1 and 32 characters.")]
    string TrackingNumber,

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Status must be between 1 and 50 characters.")]
    string Status,

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Location must be between 1 and 200 characters.")]
    string Location,

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    string? Description,

    [Required(ErrorMessage = "Timestamp is required.")]
    DateTime Timestamp);

public record UpdateTrackingStatusDto(
    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Status must be between 1 and 50 characters.")]
    string Status,

    [StringLength(200, ErrorMessage = "Location must not exceed 200 characters.")]
    string? Location,

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    string? Description);

public record CreateTrackingLocationDto(
    [Required(ErrorMessage = "Tracking number is required.")]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "Tracking number must be between 1 and 32 characters.")]
    string TrackingNumber,

    [Required(ErrorMessage = "Latitude is required.")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    double Latitude,

    [Required(ErrorMessage = "Longitude is required.")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    double Longitude,

    [Required(ErrorMessage = "Timestamp is required.")]
    DateTime Timestamp);
