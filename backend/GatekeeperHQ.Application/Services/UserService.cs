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

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
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
        var user = await _context.Users
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
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
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
                .Where(r => request.RoleIds.Contains(r.Id))
                .ToListAsync();

            var userRoles = roles.Select(r => new UserRole
            {
                UserId = user.Id,
                RoleId = r.Id
            }).ToList();

            _context.UserRoles.AddRange(userRoles);
            await _context.SaveChangesAsync();
        }

        return await GetUserByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to create user");
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return null;

        // Check email uniqueness if changing email
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
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
                    .Where(r => request.RoleIds.Contains(r.Id))
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

        return await GetUserByIdAsync(id);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
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
