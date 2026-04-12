using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/roles")]
public class RolesController(IAuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles() => Ok(await authService.GetRolesAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRole(int id)
    {
        var role = await authService.GetRoleAsync(id);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto request)
    {
        var result = await authService.CreateRoleAsync(request);
        return result.Ok ? Ok(result.Data) : Conflict(new { message = result.Message });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var result = await authService.DeleteRoleAsync(id);
        return result.Ok
            ? Ok(new { message = "Successfully deleted." })
            : BadRequest(new { message = result.Message });
    }
}
