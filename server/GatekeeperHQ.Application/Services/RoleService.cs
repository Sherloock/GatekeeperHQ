using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(int id);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request);
    Task<bool> DeleteRoleAsync(int id);
    Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId);
    Task<bool> AddPermissionToRoleAsync(int roleId, int permissionId);
    Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId);
}

public class RoleService : IRoleService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IWebhookService? _webhookService;

    public RoleService(AppDbContext context, ITenantContext tenantContext, IWebhookService? webhookService = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _webhookService = webhookService;
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var roles = await _context.Roles
            .Where(r => r.TenantId == _tenantContext.TenantId.Value)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.Key).ToList()
        }).ToList();
    }

    public async Task<RoleDto?> GetRoleByIdAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var role = await _context.Roles
            .Where(r => r.TenantId == _tenantContext.TenantId.Value)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return null;

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            Permissions = role.RolePermissions.Select(rp => rp.Permission.Key).ToList()
        };
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        // Check if role name already exists in this tenant
        if (await _context.Roles.AnyAsync(r => r.Name == request.Name && r.TenantId == _tenantContext.TenantId.Value))
        {
            throw new InvalidOperationException("Role name already exists");
        }

        var role = new Role
        {
            TenantId = _tenantContext.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Assign permissions
        if (request.PermissionIds.Any())
        {
            var permissions = await _context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id) && p.TenantId == _tenantContext.TenantId.Value)
                .ToListAsync();

            var rolePermissions = permissions.Select(p => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = p.Id
            }).ToList();

            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();
        }

        var result = await GetRoleByIdAsync(role.Id) ?? throw new InvalidOperationException("Failed to create role");

        // Trigger webhook
        if (_webhookService != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.RoleCreated,
                new { role = result }));
        }

        return result;
    }

    public async Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var role = await _context.Roles
            .Where(r => r.TenantId == _tenantContext.TenantId.Value)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return null;

        // Check name uniqueness if changing name
        if (!string.IsNullOrEmpty(request.Name) && request.Name != role.Name)
        {
            if (await _context.Roles.AnyAsync(r => r.Name == request.Name && r.TenantId == _tenantContext.TenantId.Value))
            {
                throw new InvalidOperationException("Role name already exists");
            }
            role.Name = request.Name;
        }

        if (request.Description != null)
        {
            role.Description = request.Description;
        }

        // Update permissions if provided
        if (request.PermissionIds != null)
        {
            // Remove existing permissions
            var existingRolePermissions = _context.RolePermissions.Where(rp => rp.RoleId == id);
            _context.RolePermissions.RemoveRange(existingRolePermissions);

            // Add new permissions
            if (request.PermissionIds.Any())
            {
                var permissions = await _context.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id) && p.TenantId == _tenantContext.TenantId.Value)
                    .ToListAsync();

                var rolePermissions = permissions.Select(p => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id
                }).ToList();

                _context.RolePermissions.AddRange(rolePermissions);
            }
        }

        await _context.SaveChangesAsync();

        var result = await GetRoleByIdAsync(id);

        // Trigger webhook
        if (_webhookService != null && result != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.RoleUpdated,
                new { role = result }));
        }

        return result;
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _tenantContext.TenantId.Value);
        if (role == null)
            return false;

        var roleId = role.Id;
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        // Trigger webhook
        if (_webhookService != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.RoleDeleted,
                new { roleId }));
        }

        return true;
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var role = await _context.Roles
            .Where(r => r.TenantId == _tenantContext.TenantId.Value)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
            return new List<PermissionDto>();

        return role.RolePermissions
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Key = rp.Permission.Key,
                Description = rp.Permission.Description
            })
            .ToList();
    }

    public async Task<bool> AddPermissionToRoleAsync(int roleId, int permissionId)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == _tenantContext.TenantId.Value);
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId && p.TenantId == _tenantContext.TenantId.Value);

        if (role == null || permission == null)
            return false;

        // Check if already exists
        if (await _context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            return false;

        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        // Trigger webhook
        if (_webhookService != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.PermissionGranted,
                new { roleId, permissionId }));
        }

        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        // Verify role and permission belong to tenant
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == _tenantContext.TenantId.Value);
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId && p.TenantId == _tenantContext.TenantId.Value);

        if (role == null || permission == null)
            return false;

        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
            return false;

        var revokedRoleId = rolePermission.RoleId;
        var revokedPermissionId = rolePermission.PermissionId;
        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();

        // Trigger webhook
        if (_webhookService != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.PermissionRevoked,
                new { roleId = revokedRoleId, permissionId = revokedPermissionId }));
        }

        return true;
    }
}

// DTOs for service layer
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<int>? PermissionIds { get; set; }
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
}
