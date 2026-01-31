using GatekeeperHQ.API.DTOs.Tenants;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class TenantsController : ControllerBase
{
	private readonly ITenantService _tenantService;

	public TenantsController(ITenantService tenantService)
	{
		_tenantService = tenantService;
	}

	[HttpGet]
	[Authorize(Policy = Permissions.TenantsView)]
	public async Task<ActionResult<List<DTOs.Tenants.TenantDto>>> GetTenants()
	{
		var tenants = await _tenantService.GetAllTenantsAsync();
		var result = tenants.Select(t => new DTOs.Tenants.TenantDto
		{
			Id = t.Id,
			Name = t.Name,
			ApiKey = t.ApiKey,
			IsActive = t.IsActive,
			CreatedAt = t.CreatedAt,
			UpdatedAt = t.UpdatedAt
		}).ToList();
		return Ok(result);
	}

	[HttpGet("{id}")]
	[Authorize(Policy = Permissions.TenantsView)]
	public async Task<ActionResult<DTOs.Tenants.TenantDto>> GetTenant(int id)
	{
		var tenant = await _tenantService.GetTenantByIdAsync(id);
		if (tenant == null)
			return NotFound(new { message = "Tenant not found" });

		var result = new DTOs.Tenants.TenantDto
		{
			Id = tenant.Id,
			Name = tenant.Name,
			ApiKey = tenant.ApiKey,
			IsActive = tenant.IsActive,
			CreatedAt = tenant.CreatedAt,
			UpdatedAt = tenant.UpdatedAt
		};
		return Ok(result);
	}

	[HttpPost]
	[Authorize(Policy = Permissions.TenantsCreate)]
	public async Task<ActionResult<DTOs.Tenants.TenantDto>> CreateTenant([FromBody] DTOs.Tenants.CreateTenantRequest request)
	{
		try
		{
			var tenant = await _tenantService.CreateTenantAsync(new Application.Services.CreateTenantRequest
			{
				Name = request.Name,
				IsActive = request.IsActive
			});

			var result = new DTOs.Tenants.TenantDto
			{
				Id = tenant.Id,
				Name = tenant.Name,
				ApiKey = tenant.ApiKey,
				IsActive = tenant.IsActive,
				CreatedAt = tenant.CreatedAt,
				UpdatedAt = tenant.UpdatedAt
			};
			return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, result);
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	[HttpPut("{id}")]
	[Authorize(Policy = Permissions.TenantsManage)]
	public async Task<ActionResult<DTOs.Tenants.TenantDto>> UpdateTenant(int id, [FromBody] DTOs.Tenants.UpdateTenantRequest request)
	{
		try
		{
			var tenant = await _tenantService.UpdateTenantAsync(id, new Application.Services.UpdateTenantRequest
			{
				Name = request.Name,
				IsActive = request.IsActive
			});

			if (tenant == null)
				return NotFound(new { message = "Tenant not found" });

			var result = new DTOs.Tenants.TenantDto
			{
				Id = tenant.Id,
				Name = tenant.Name,
				ApiKey = tenant.ApiKey,
				IsActive = tenant.IsActive,
				CreatedAt = tenant.CreatedAt,
				UpdatedAt = tenant.UpdatedAt
			};
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	[HttpDelete("{id}")]
	[Authorize(Policy = Permissions.TenantsManage)]
	public async Task<IActionResult> DeleteTenant(int id)
	{
		var result = await _tenantService.DeleteTenantAsync(id);
		if (!result)
			return NotFound(new { message = "Tenant not found" });

		return NoContent();
	}

	[HttpPost("{id}/regenerate-api-key")]
	[Authorize(Policy = Permissions.TenantsManage)]
	public async Task<ActionResult<RegenerateApiKeyResponse>> RegenerateApiKey(int id)
	{
		try
		{
			var apiKey = await _tenantService.RegenerateApiKeyAsync(id);
			return Ok(new RegenerateApiKeyResponse { ApiKey = apiKey });
		}
		catch (InvalidOperationException ex)
		{
			return NotFound(new { message = ex.Message });
		}
	}
}
