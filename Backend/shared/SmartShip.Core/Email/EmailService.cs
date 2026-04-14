using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartShip.Core.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _emailSettings = new EmailSettings();
        configuration.GetSection("EmailSettings").Bind(_emailSettings);
    }

    public async Task<bool> SendOtpEmailAsync(string recipientEmail, string otp)
    {
        var subject = "Your OTP Code";
        var body = $@"
                <h2>SmartShip OTP Verification</h2>
                <p>Your OTP code is: <strong>{otp}</strong></p>
                <p>This OTP will expire in 5 minutes.</p>
                <p>If you did not request this OTP, please ignore this email.</p>
            ";

        return await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string otp)
    {
        var subject = "Password Reset Request";
        var body = $@"
                <h2>SmartShip Password Reset</h2>
                <p>Your password reset OTP code is: <strong>{otp}</strong></p>
                <p>This OTP will expire in 5 minutes.</p>
                <p>If you did not request this, please ignore this email and your password will remain unchanged.</p>
            ";

        return await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task<bool> SendPickupScheduledEmailAsync(string recipientEmail, string trackingNumber, DateTime pickupDate, string? notes = null)
    {
        var subject = "Pickup Scheduled for Your Shipment";
        var formattedPickupDate = pickupDate.ToString("f");
        var notesBlock = string.IsNullOrWhiteSpace(notes)
            ? string.Empty
            : $"<p><strong>Notes:</strong> {notes.Trim()}</p>";

        var body = $@"
                <h2>SmartShip Pickup Scheduled</h2>
                <p>Your pickup has been scheduled successfully.</p>
                <p><strong>Tracking Number:</strong> {trackingNumber}</p>
                <p><strong>Pickup Date & Time:</strong> {formattedPickupDate}</p>
                {notesBlock}
                <p>You can track or manage your shipment anytime from your SmartShip account.</p>
            ";

        return await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task<bool> SendPickupUpdatedEmailAsync(string recipientEmail, string trackingNumber, DateTime pickupDate, string? notes = null)
    {
        var subject = "Pickup Updated for Your Shipment";
        var formattedPickupDate = pickupDate.ToString("f");
        var notesBlock = string.IsNullOrWhiteSpace(notes)
            ? string.Empty
            : $"<p><strong>Updated Notes:</strong> {notes.Trim()}</p>";

        var body = $@"
                <h2>SmartShip Pickup Updated</h2>
                <p>Your pickup details have been updated.</p>
                <p><strong>Tracking Number:</strong> {trackingNumber}</p>
                <p><strong>Pickup Date & Time:</strong> {formattedPickupDate}</p>
                {notesBlock}
                <p>Please review your shipment details in your SmartShip account.</p>
            ";

        return await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task<bool> SendShipmentStatusEmailAsync(string recipientEmail, string trackingNumber, string status, string message)
    {
        var subject = $"Shipment Update: {status}";
        var body = $@"
                <h2>SmartShip Shipment Status Update</h2>
                <p><strong>Tracking Number:</strong> {trackingNumber}</p>
                <p><strong>Current Status:</strong> {status}</p>
                <p>{message}</p>
                <p>You can check live status in your SmartShip account.</p>
            ";

        return await SendEmailAsync(recipientEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.Port)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(recipientEmail);
            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Email}", recipientEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", recipientEmail);
            return false;
        }
    }
}

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "SmartShip Logistics";
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}