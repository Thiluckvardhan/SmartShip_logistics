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
                From = new MailAddress(_emailSettings.SenderEmail),
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
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}