using System.ComponentModel.DataAnnotations;

namespace SmartShip.IdentityService.DTOs;

public record LoginDto(
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    string Email,

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    string Password);

public record TokenDto(
    [Required(ErrorMessage = "Token is required.")]
    string Token);

public record GoogleLoginDto(
    [Required(ErrorMessage = "Google ID token is required.")]
    string IdToken);

public record ForgotPasswordDto(
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    string Email);

public record ResetPasswordDto(
    [Required(ErrorMessage = "Token is required.")]
    string Token,

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(100, ErrorMessage = "Password must not exceed 100 characters.")]
    string NewPassword);

public record CreateRoleDto(
    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters.")]
    string RoleName);

public record AssignUserRoleDto(
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    string Email,

    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters.")]
    string RoleName);