using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
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
                logger.LogInformation("User {Email} logged in successfully. CorrelationId: {CorrelationId}", email, HttpContext.TraceIdentifier);
                return Ok(result.Data);
            }

            logger.LogWarning("Invalid login attempt for {Email}. CorrelationId: {CorrelationId}", email, HttpContext.TraceIdentifier);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed for {Email}. CorrelationId: {CorrelationId}", email, HttpContext.TraceIdentifier);
            throw;
        }
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