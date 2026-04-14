using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("google-config")]
    public IActionResult GetGoogleConfig()
    {
        var configuredClientIds = configuration
            .GetSection("GoogleAuth:ClientIds")
            .Get<string[]>()?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        var clientId = configuredClientIds.FirstOrDefault()
            ?? (configuration["GoogleAuth:ClientId"] ?? string.Empty);

        return Ok(new
        {
            clientId
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var result = await authService.RegisterAsync(request);
        return result.Ok ? Ok(result.Data) : BadRequest(new { message = result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        try
        {
            var result = await authService.LoginAsync(request);
            if (result.Ok)
            {
                logger.LogInformation("Login challenge started for {Email}. CorrelationId: {CorrelationId}", email, HttpContext.TraceIdentifier);
                return Ok(result.Data);
            }

            logger.LogWarning("Invalid login attempt for {Email}. CorrelationId: {CorrelationId}", email, HttpContext.TraceIdentifier);
            return Unauthorized(new { message = result.Message ?? "Invalid credentials." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed for {Email}. CorrelationId: {CorrelationId}", email, HttpContext.TraceIdentifier);
            throw;
        }
    }

    [HttpPost("verify-login-otp")]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpDto request)
    {
        var result = await authService.VerifyLoginOtpAsync(request);
        return result.Ok
            ? Ok(result.Data)
            : Unauthorized(new { message = result.Message ?? "Invalid or expired OTP." });
    }

    [HttpPost("resend-login-otp")]
    public async Task<IActionResult> ResendLoginOtp([FromBody] ResendLoginOtpDto request)
    {
        var result = await authService.ResendLoginOtpAsync(request);
        if (result.Ok)
        {
            return Ok(result.Data);
        }

        if (result.Data is not null)
        {
            return StatusCode(429, result.Data);
        }

        return Unauthorized(new { message = result.Message ?? "Invalid or expired OTP challenge." });
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
    {
        try
        {
            var result = await authService.LoginWithGoogleAsync(request);
            if (result.Ok)
            {
                logger.LogInformation("Google login succeeded. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
                return Ok(result.Data);
            }

            logger.LogWarning("Google login failed. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return Unauthorized(new { message = result.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google login error. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            throw;
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] TokenDto request)
    {
        var result = await authService.RefreshTokenAsync(request);
        return result.Ok ? Ok(result.Data) : Unauthorized();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] TokenDto request)
    {
        var result = await authService.LogoutAsync(request);
        return result.Ok ? NoContent() : NotFound(new { message = result.Message });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        var result = await authService.ForgotPasswordAsync(request);
        return Ok(result.Data ?? new { message = result.Message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var result = await authService.ResetPasswordAsync(request);
        return result.Ok ? Ok(new { message = "Password reset successful." }) : BadRequest(new { message = result.Message });
    }
}