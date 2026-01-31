using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IWebhookService? _webhookService;

    public UserService(AppDbContext context, ITenantContext tenantContext, IWebhookService? webhookService = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _webhookService = webhookService;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var users = await _context.Users
            .Where(u => u.TenantId == _tenantContext.TenantId.Value)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var user = await _context.Users
            .Where(u => u.TenantId == _tenantContext.TenantId.Value)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        // Check if email already exists in this tenant
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.TenantId == _tenantContext.TenantId.Value))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            TenantId = _tenantContext.TenantId.Value,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assign roles
        if (request.RoleIds.Any())
        {
            var roles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.Id) && r.TenantId == _tenantContext.TenantId.Value)
                .ToListAsync();

            var userRoles = roles.Select(r => new UserRole
            {
                UserId = user.Id,
                RoleId = r.Id
            }).ToList();

            _context.UserRoles.AddRange(userRoles);
            await _context.SaveChangesAsync();
        }

        var result = await GetUserByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to create user");

        // Trigger webhook
        if (_webhookService != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.UserCreated,
                new { user = result }));
        }

        return result;
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var user = await _context.Users
            .Where(u => u.TenantId == _tenantContext.TenantId.Value)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return null;

        // Check email uniqueness if changing email
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.TenantId == _tenantContext.TenantId.Value))
            {
                throw new InvalidOperationException("Email already exists");
            }
            user.Email = request.Email;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        // Update roles if provided
        if (request.RoleIds != null)
        {
            // Remove existing roles
            var existingUserRoles = _context.UserRoles.Where(ur => ur.UserId == id);
            _context.UserRoles.RemoveRange(existingUserRoles);

            // Add new roles
            if (request.RoleIds.Any())
            {
                var roles = await _context.Roles
                    .Where(r => request.RoleIds.Contains(r.Id) && r.TenantId == _tenantContext.TenantId.Value)
                    .ToListAsync();

                var userRoles = roles.Select(r => new UserRole
                {
                    UserId = user.Id,
                    RoleId = r.Id
                }).ToList();

                _context.UserRoles.AddRange(userRoles);
            }
        }

        await _context.SaveChangesAsync();

        var result = await GetUserByIdAsync(id);

        // Trigger webhook
        if (_webhookService != null && result != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.UserUpdated,
                new { user = result }));
        }

        return result;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _tenantContext.TenantId.Value);
        if (user == null)
            return false;

        var userId = user.Id;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Trigger webhook
        if (_webhookService != null)
        {
            _ = Task.Run(async () => await _webhookService.TriggerWebhookAsync(
                Domain.Entities.WebhookEvents.UserDeleted,
                new { userId }));
        }

        return true;
    }
}

// DTOs for service layer
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<int> RoleIds { get; set; } = new();
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool? IsActive { get; set; }
    public List<int>? RoleIds { get; set; }
}
