using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IPermissionService
{
	Task<List<PermissionDto>> GetAllAsync();
	Task<PermissionDto?> GetByIdAsync(int id);
	Task<PermissionDto> CreateAsync(CreatePermissionRequest request);
	Task<PermissionDto?> UpdateAsync(int id, UpdatePermissionRequest request);
	Task<bool> DeleteAsync(int id);
}

public class PermissionService : IPermissionService
{
	private readonly AppDbContext _context;
	private readonly ITenantContext _tenantContext;

	public PermissionService(AppDbContext context, ITenantContext tenantContext)
	{
		_context = context;
		_tenantContext = tenantContext;
	}

	public async Task<List<PermissionDto>> GetAllAsync()
	{
		if (!_tenantContext.TenantId.HasValue)
		{
			throw new InvalidOperationException("Tenant context is required");
		}

		var permissions = await _context.Permissions
			.Where(p => p.TenantId == _tenantContext.TenantId.Value)
			.OrderBy(p => p.Key)
			.ToListAsync();

		return permissions.Select(p => new PermissionDto
		{
			Id = p.Id,
			Key = p.Key,
			Description = p.Description
		}).ToList();
	}

	public async Task<PermissionDto?> GetByIdAsync(int id)
	{
		if (!_tenantContext.TenantId.HasValue)
		{
			throw new InvalidOperationException("Tenant context is required");
		}

		var permission = await _context.Permissions
			.Where(p => p.TenantId == _tenantContext.TenantId.Value)
			.FirstOrDefaultAsync(p => p.Id == id);

		if (permission == null)
			return null;

		return new PermissionDto
		{
			Id = permission.Id,
			Key = permission.Key,
			Description = permission.Description
		};
	}

	public async Task<PermissionDto> CreateAsync(CreatePermissionRequest request)
	{
		if (!_tenantContext.TenantId.HasValue)
		{
			throw new InvalidOperationException("Tenant context is required");
		}

		// Validate key format (should be like "resource.action")
		if (string.IsNullOrWhiteSpace(request.Key) || !request.Key.Contains('.'))
		{
			throw new InvalidOperationException("Permission key must be in format 'resource.action' (e.g., 'reports.view')");
		}

		// Check if permission key already exists in this tenant
		if (await _context.Permissions.AnyAsync(p => p.Key == request.Key && p.TenantId == _tenantContext.TenantId.Value))
		{
			throw new InvalidOperationException("Permission key already exists");
		}

		var permission = new Permission
		{
			TenantId = _tenantContext.TenantId.Value,
			Key = request.Key.ToLower(),
			Description = request.Description
		};

		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		return new PermissionDto
		{
			Id = permission.Id,
			Key = permission.Key,
			Description = permission.Description
		};
	}

	public async Task<PermissionDto?> UpdateAsync(int id, UpdatePermissionRequest request)
	{
		if (!_tenantContext.TenantId.HasValue)
		{
			throw new InvalidOperationException("Tenant context is required");
		}

		var permission = await _context.Permissions
			.Where(p => p.TenantId == _tenantContext.TenantId.Value)
			.FirstOrDefaultAsync(p => p.Id == id);

		if (permission == null)
			return null;

		// Update key if provided
		if (!string.IsNullOrEmpty(request.Key) && request.Key != permission.Key)
		{
			// Validate key format
			if (!request.Key.Contains('.'))
			{
				throw new InvalidOperationException("Permission key must be in format 'resource.action' (e.g., 'reports.view')");
			}

			// Check uniqueness
			if (await _context.Permissions.AnyAsync(p => p.Key == request.Key && p.TenantId == _tenantContext.TenantId.Value))
			{
				throw new InvalidOperationException("Permission key already exists");
			}

			permission.Key = request.Key.ToLower();
		}

		// Update description if provided (can be set to null)
		if (request.Description != null)
		{
			permission.Description = request.Description;
		}

		await _context.SaveChangesAsync();

		return new PermissionDto
		{
			Id = permission.Id,
			Key = permission.Key,
			Description = permission.Description
		};
	}

	public async Task<bool> DeleteAsync(int id)
	{
		if (!_tenantContext.TenantId.HasValue)
		{
			throw new InvalidOperationException("Tenant context is required");
		}

		var permission = await _context.Permissions
			.Where(p => p.TenantId == _tenantContext.TenantId.Value)
			.FirstOrDefaultAsync(p => p.Id == id);

		if (permission == null)
			return false;

		// Check if permission is in use by any role
		var isInUse = await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == id);
		if (isInUse)
		{
			throw new InvalidOperationException("Cannot delete permission that is assigned to roles. Remove it from all roles first.");
		}

		_context.Permissions.Remove(permission);
		await _context.SaveChangesAsync();

		return true;
	}
}

// Request DTOs for service layer
public class CreatePermissionRequest
{
	public string Key { get; set; } = string.Empty;
	public string? Description { get; set; }
}

public class UpdatePermissionRequest
{
	public string? Key { get; set; }
	public string? Description { get; set; }
}
