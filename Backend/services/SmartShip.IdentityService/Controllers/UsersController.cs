using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController(IAuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await authService.GetAllUsersAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await authService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("by-email")]
    public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
    {
        var user = await authService.GetUserByEmailAsync(email);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto request)
    {
        var result = await authService.UpdateUserRoleAsync(id, request.RoleName);
        return result.Ok ? Ok(new { message = "Role updated." }) : BadRequest(new { message = result.Message });
    }

    [HttpPut("by-email/role")]
    public async Task<IActionResult> UpdateUserRoleByEmail([FromQuery] string email, [FromBody] UpdateUserRoleDto request)
    {
        var result = await authService.UpdateUserRoleByEmailAsync(email, request.RoleName);
        return result.Ok ? Ok(new { message = "Role updated." }) : BadRequest(new { message = result.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var deleted = await authService.DeleteUserAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpDelete("by-email")]
    public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
    {
        var deleted = await authService.DeleteUserByEmailAsync(email);
        return deleted ? NoContent() : NotFound();
    }
}
