using GatekeeperHQ.API.DTOs.Roles;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesView)]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.RolesView)]
    public async Task<ActionResult<RoleDto>> GetRole(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        return Ok(role);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var role = await _roleService.CreateRoleAsync(new Application.Services.CreateRoleRequest
            {
                Name = request.Name,
                Description = request.Description,
                PermissionIds = request.PermissionIds
            });

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _roleService.UpdateRoleAsync(id, new Application.Services.UpdateRoleRequest
            {
                Name = request.Name,
                Description = request.Description,
                PermissionIds = request.PermissionIds
            });

            if (role == null)
                return NotFound(new { message = "Role not found" });

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var deleted = await _roleService.DeleteRoleAsync(id);
        if (!deleted)
            return NotFound(new { message = "Role not found" });

        return NoContent();
    }

    [HttpGet("{id}/permissions")]
    [Authorize(Policy = Permissions.RolesView)]
    public async Task<ActionResult<List<PermissionDto>>> GetRolePermissions(int id)
    {
        var permissions = await _roleService.GetRolePermissionsAsync(id);
        return Ok(permissions);
    }

    [HttpPost("{id}/permissions")]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<IActionResult> AddPermissionToRole(int id, [FromBody] AddPermissionRequest request)
    {
        var added = await _roleService.AddPermissionToRoleAsync(id, request.PermissionId);
        if (!added)
            return BadRequest(new { message = "Failed to add permission. Role or permission not found, or permission already assigned." });

        return NoContent();
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<IActionResult> RemovePermissionFromRole(int id, int permissionId)
    {
        var removed = await _roleService.RemovePermissionFromRoleAsync(id, permissionId);
        if (!removed)
            return NotFound(new { message = "Permission not found on role" });

        return NoContent();
    }
}
