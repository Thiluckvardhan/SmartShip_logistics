using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class ProfileController(IAuthService authService, ILogger<ProfileController> logger) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var user = await authService.GetCurrentUserAsync(userId);
            if (user is null) return NotFound();

            logger.LogInformation("User {UserId} fetched profile. CorrelationId: {CorrelationId}", userId, HttpContext.TraceIdentifier);
            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch profile for user {UserId}. CorrelationId: {CorrelationId}", userId, HttpContext.TraceIdentifier);
            throw;
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await authService.UpdateCurrentUserAsync(userId, request);
        if (!result.Ok)
        {
            return result.Message == "User not found." ? NotFound(new { message = result.Message }) : Conflict(new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var deleted = await authService.DeleteCurrentUserAsync(userId);
        return deleted ? NoContent() : NotFound();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }
}
