using GatekeeperHQ.API.Filters;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiCreatePermissionRequest = GatekeeperHQ.API.DTOs.Permissions.CreatePermissionRequest;
using ApiPermissionDto = GatekeeperHQ.API.DTOs.Permissions.PermissionDto;
using ApiUpdatePermissionRequest = GatekeeperHQ.API.DTOs.Permissions.UpdatePermissionRequest;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[RequireTenantContext]
public class PermissionsController : ControllerBase
{
	private readonly IPermissionService _permissionService;

	public PermissionsController(IPermissionService permissionService)
	{
		_permissionService = permissionService;
	}

	[HttpGet]
	[Authorize(Policy = Permissions.PermissionsView)]
	public async Task<ActionResult<List<ApiPermissionDto>>> GetPermissions()
	{
		try
		{
			var permissions = await _permissionService.GetAllAsync();
			var result = permissions.Select(p => new ApiPermissionDto
			{
				Id = p.Id,
				Key = p.Key,
				Description = p.Description
			}).ToList();
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpGet("{id}")]
	[Authorize(Policy = Permissions.PermissionsView)]
	public async Task<ActionResult<ApiPermissionDto>> GetPermission(int id)
	{
		try
		{
			var permission = await _permissionService.GetByIdAsync(id);
			if (permission == null)
				return NotFound(new { message = "Permission not found" });

			return Ok(new ApiPermissionDto
			{
				Id = permission.Id,
				Key = permission.Key,
				Description = permission.Description
			});
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost]
	[Authorize(Policy = Permissions.PermissionsCreate)]
	public async Task<ActionResult<ApiPermissionDto>> CreatePermission([FromBody] ApiCreatePermissionRequest request)
	{
		try
		{
			var permission = await _permissionService.CreateAsync(new CreatePermissionRequest
			{
				Key = request.Key,
				Description = request.Description
			});

			var result = new ApiPermissionDto
			{
				Id = permission.Id,
				Key = permission.Key,
				Description = permission.Description
			};

			return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, result);
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	[HttpPut("{id}")]
	[Authorize(Policy = Permissions.PermissionsManage)]
	public async Task<ActionResult<ApiPermissionDto>> UpdatePermission(int id, [FromBody] ApiUpdatePermissionRequest request)
	{
		try
		{
			var permission = await _permissionService.UpdateAsync(id, new UpdatePermissionRequest
			{
				Key = request.Key,
				Description = request.Description
			});

			if (permission == null)
				return NotFound(new { message = "Permission not found" });

			return Ok(new ApiPermissionDto
			{
				Id = permission.Id,
				Key = permission.Key,
				Description = permission.Description
			});
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	[HttpDelete("{id}")]
	[Authorize(Policy = Permissions.PermissionsManage)]
	public async Task<IActionResult> DeletePermission(int id)
	{
		try
		{
			var result = await _permissionService.DeleteAsync(id);
			if (!result)
				return NotFound(new { message = "Permission not found" });

			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}
}
