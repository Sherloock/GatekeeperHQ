using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Auth;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password);
    Task<UserWithPermissions?> GetUserWithPermissionsAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthService(AppDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        var token = _jwtService.GenerateToken(user.Id, user.Email, permissions);

        return new AuthResult
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Permissions = permissions
        };
    }

    public async Task<UserWithPermissions?> GetUserWithPermissionsAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
            return null;

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .ToList();

        return new UserWithPermissions
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = roles,
            Permissions = permissions
        };
    }
}

public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}

public class UserWithPermissions
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
