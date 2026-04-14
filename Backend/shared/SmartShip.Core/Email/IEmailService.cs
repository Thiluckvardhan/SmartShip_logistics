namespace SmartShip.Core.Email;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string recipientEmail, string otp);
    Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string otp);
    Task<bool> SendPickupScheduledEmailAsync(string recipientEmail, string trackingNumber, DateTime pickupDate, string? notes = null);
    Task<bool> SendPickupUpdatedEmailAsync(string recipientEmail, string trackingNumber, DateTime pickupDate, string? notes = null);
    Task<bool> SendShipmentStatusEmailAsync(string recipientEmail, string trackingNumber, string status, string message);
}