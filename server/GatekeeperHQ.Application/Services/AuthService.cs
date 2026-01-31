using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Auth;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password);
    Task<UserWithPermissions?> GetUserWithPermissionsAsync(int userId);
    Task<AuthResult?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ITenantContext _tenantContext;

    public AuthService(AppDbContext context, JwtService jwtService, ITenantContext tenantContext)
    {
        _context = context;
        _jwtService = jwtService;
        _tenantContext = tenantContext;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return null;
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == _tenantContext.TenantId.Value && u.IsActive);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.TenantId, permissions);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResult
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email,
            Permissions = permissions
        };
    }

    public async Task<UserWithPermissions?> GetUserWithPermissionsAsync(int userId)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return null;
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantContext.TenantId.Value && u.IsActive);

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

    public async Task<AuthResult?> RefreshTokenAsync(string refreshToken)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return null;
        }

        var tokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken
                && rt.TenantId == _tenantContext.TenantId.Value
                && !rt.IsRevoked
                && rt.ExpiresAt > DateTime.UtcNow);

        if (tokenEntity == null || tokenEntity.User == null || !tokenEntity.User.IsActive)
        {
            return null;
        }

        // Revoke old refresh token
        tokenEntity.IsRevoked = true;

        // Generate new tokens
        var permissions = tokenEntity.User.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        var newToken = _jwtService.GenerateToken(tokenEntity.User.Id, tokenEntity.User.Email, tokenEntity.User.TenantId, permissions);
        var newRefreshToken = GenerateRefreshToken();

        // Store new refresh token
        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            UserId = tokenEntity.User.Id,
            TenantId = tokenEntity.User.TenantId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResult
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            UserId = tokenEntity.User.Id,
            Email = tokenEntity.User.Email,
            Permissions = permissions
        };
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return false;
        }

        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken
                && rt.TenantId == _tenantContext.TenantId.Value
                && !rt.IsRevoked);

        if (tokenEntity == null)
        {
            return false;
        }

        tokenEntity.IsRevoked = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    }
}

public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
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
