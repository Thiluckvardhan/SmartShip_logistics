namespace SmartShip.Core.Email;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string recipientEmail, string otp);
    Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string otp);
}