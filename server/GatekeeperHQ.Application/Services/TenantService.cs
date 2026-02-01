using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface ITenantService
{
	Task<List<TenantDto>> GetAllTenantsAsync();
	Task<TenantDto?> GetTenantByIdAsync(int id);
	Task<TenantDto> CreateTenantAsync(CreateTenantRequest request);
	Task<TenantDto?> UpdateTenantAsync(int id, UpdateTenantRequest request);
	Task<bool> DeleteTenantAsync(int id);
	Task<string> RegenerateApiKeyAsync(int id);
}

public class TenantService : ITenantService
{
	private readonly AppDbContext _context;

	public TenantService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<TenantDto>> GetAllTenantsAsync()
	{
		var tenants = await _context.Tenants
			.OrderBy(t => t.Name)
			.ToListAsync();

		return tenants.Select(t => new TenantDto
		{
			Id = t.Id,
			Name = t.Name,
			ApiKey = t.ApiKey,
			IsActive = t.IsActive,
			CreatedAt = t.CreatedAt,
			UpdatedAt = t.UpdatedAt
		}).ToList();
	}

	public async Task<TenantDto?> GetTenantByIdAsync(int id)
	{
		var tenant = await _context.Tenants.FindAsync(id);
		if (tenant == null)
			return null;

		return new TenantDto
		{
			Id = tenant.Id,
			Name = tenant.Name,
			ApiKey = tenant.ApiKey,
			IsActive = tenant.IsActive,
			CreatedAt = tenant.CreatedAt,
			UpdatedAt = tenant.UpdatedAt
		};
	}

	public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request)
	{
		// Check if tenant name already exists
		if (await _context.Tenants.AnyAsync(t => t.Name == request.Name))
		{
			throw new InvalidOperationException("Tenant name already exists");
		}

		var tenant = new Tenant
		{
			Name = request.Name,
			ApiKey = Guid.NewGuid().ToString("N"),
			IsActive = request.IsActive,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		// Seed default permissions for new tenant
		await SeedTenantPermissionsAsync(tenant.Id);

		return await GetTenantByIdAsync(tenant.Id) ?? throw new InvalidOperationException("Failed to create tenant");
	}

	public async Task<TenantDto?> UpdateTenantAsync(int id, UpdateTenantRequest request)
	{
		var tenant = await _context.Tenants.FindAsync(id);
		if (tenant == null)
			return null;

		// Check name uniqueness if changing name
		if (!string.IsNullOrEmpty(request.Name) && request.Name != tenant.Name)
		{
			if (await _context.Tenants.AnyAsync(t => t.Name == request.Name))
			{
				throw new InvalidOperationException("Tenant name already exists");
			}
			tenant.Name = request.Name;
		}

		if (request.IsActive.HasValue)
		{
			tenant.IsActive = request.IsActive.Value;
		}

		tenant.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return await GetTenantByIdAsync(id);
	}

	public async Task<bool> DeleteTenantAsync(int id)
	{
		var tenant = await _context.Tenants.FindAsync(id);
		if (tenant == null)
			return false;

		// Delete related entities in correct order to avoid FK violations
		// 1. Delete UserRoles for users in this tenant
		var userIds = await _context.Users
			.Where(u => u.TenantId == id)
			.Select(u => u.Id)
			.ToListAsync();
		if (userIds.Count > 0)
		{
			var userRoles = await _context.UserRoles
				.Where(ur => userIds.Contains(ur.UserId))
				.ToListAsync();
			_context.UserRoles.RemoveRange(userRoles);
		}

		// 2. Delete RefreshTokens for users in this tenant
		var refreshTokens = await _context.RefreshTokens
			.Where(rt => rt.TenantId == id)
			.ToListAsync();
		_context.RefreshTokens.RemoveRange(refreshTokens);

		// 3. Delete RolePermissions for roles in this tenant
		var roleIds = await _context.Roles
			.Where(r => r.TenantId == id)
			.Select(r => r.Id)
			.ToListAsync();
		if (roleIds.Count > 0)
		{
			var rolePermissions = await _context.RolePermissions
				.Where(rp => roleIds.Contains(rp.RoleId))
				.ToListAsync();
			_context.RolePermissions.RemoveRange(rolePermissions);
		}

		// 4. Delete Invitations for this tenant
		var invitations = await _context.Invitations
			.Where(i => i.TenantId == id)
			.ToListAsync();
		_context.Invitations.RemoveRange(invitations);

		// 5. Delete Webhooks for this tenant
		var webhooks = await _context.Webhooks
			.Where(w => w.TenantId == id)
			.ToListAsync();
		_context.Webhooks.RemoveRange(webhooks);

		// 6. Delete ApiKeys for this tenant
		var apiKeys = await _context.ApiKeys
			.Where(a => a.TenantId == id)
			.ToListAsync();
		_context.ApiKeys.RemoveRange(apiKeys);

		// 7. Delete Users in this tenant
		var users = await _context.Users
			.Where(u => u.TenantId == id)
			.ToListAsync();
		_context.Users.RemoveRange(users);

		// 8. Delete Roles in this tenant
		var roles = await _context.Roles
			.Where(r => r.TenantId == id)
			.ToListAsync();
		_context.Roles.RemoveRange(roles);

		// 9. Delete Permissions in this tenant
		var permissions = await _context.Permissions
			.Where(p => p.TenantId == id)
			.ToListAsync();
		_context.Permissions.RemoveRange(permissions);

		// 10. Finally delete the tenant
		_context.Tenants.Remove(tenant);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<string> RegenerateApiKeyAsync(int id)
	{
		var tenant = await _context.Tenants.FindAsync(id);
		if (tenant == null)
			throw new InvalidOperationException("Tenant not found");

		tenant.ApiKey = Guid.NewGuid().ToString("N");
		tenant.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return tenant.ApiKey;
	}

	private async Task SeedTenantPermissionsAsync(int tenantId)
	{
		// Get tenant-level permission keys (not Super Admin permissions)
		var permissionKeys = Domain.Constants.Permissions.TenantPermissions;

		var permissions = permissionKeys.Select(key => new Permission
		{
			TenantId = tenantId,
			Key = key,
			Description = GetPermissionDescription(key)
		}).ToList();

		await _context.Permissions.AddRangeAsync(permissions);
		await _context.SaveChangesAsync();
	}

	private static string GetPermissionDescription(string key)
	{
		return key switch
		{
			Domain.Constants.Permissions.UsersView => "View users list and details",
			Domain.Constants.Permissions.UsersEdit => "Edit user information",
			Domain.Constants.Permissions.UsersDelete => "Delete users",
			Domain.Constants.Permissions.UsersCreate => "Create new users",
			Domain.Constants.Permissions.RolesView => "View roles list and details",
			Domain.Constants.Permissions.RolesManage => "Create, edit, and delete roles",
			Domain.Constants.Permissions.PermissionsView => "View available permissions",
			Domain.Constants.Permissions.DashboardAccess => "Access dashboard",
			Domain.Constants.Permissions.SettingsAccess => "Access settings",
			_ => $"Permission: {key}"
		};
	}
}

// DTOs for service layer
public class TenantDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? ApiKey { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class CreateTenantRequest
{
	public string Name { get; set; } = string.Empty;
	public bool IsActive { get; set; } = true;
}

public class UpdateTenantRequest
{
	public string? Name { get; set; }
	public bool? IsActive { get; set; }
}
