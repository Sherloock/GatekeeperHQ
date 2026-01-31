using GatekeeperHQ.Domain.Constants;
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
		// First, check if this is a Super Admin login (no tenant context needed)
		var superAdmin = await _context.Users
			.FirstOrDefaultAsync(u => u.Email == email && u.IsSuperAdmin && u.IsActive);

		if (superAdmin != null)
		{
			if (!BCrypt.Net.BCrypt.Verify(password, superAdmin.PasswordHash))
				return null;

			// Super Admin gets all permissions
			var allPermissions = Permissions.All.ToList();

			var token = _jwtService.GenerateToken(superAdmin.Id, superAdmin.Email, null, allPermissions, isSuperAdmin: true);
			var refreshToken = GenerateRefreshToken();

			var refreshTokenEntity = new RefreshToken
			{
				UserId = superAdmin.Id,
				TenantId = null,
				Token = refreshToken,
				ExpiresAt = DateTime.UtcNow.AddDays(7),
				CreatedAt = DateTime.UtcNow
			};

			_context.RefreshTokens.Add(refreshTokenEntity);
			await _context.SaveChangesAsync();

			return new AuthResult
			{
				Token = token,
				RefreshToken = refreshToken,
				UserId = superAdmin.Id,
				Email = superAdmin.Email,
				Permissions = allPermissions,
				IsSuperAdmin = true
			};
		}

		// Regular tenant user login - requires tenant context
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

		var userToken = _jwtService.GenerateToken(user.Id, user.Email, user.TenantId, permissions);
		var userRefreshToken = GenerateRefreshToken();

		var userRefreshTokenEntity = new RefreshToken
		{
			UserId = user.Id,
			TenantId = user.TenantId,
			Token = userRefreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			CreatedAt = DateTime.UtcNow
		};

		_context.RefreshTokens.Add(userRefreshTokenEntity);
		await _context.SaveChangesAsync();

		return new AuthResult
		{
			Token = userToken,
			RefreshToken = userRefreshToken,
			UserId = user.Id,
			Email = user.Email,
			Permissions = permissions,
			IsSuperAdmin = false
		};
	}

	public async Task<UserWithPermissions?> GetUserWithPermissionsAsync(int userId)
	{
		// First check if this is a Super Admin
		var superAdmin = await _context.Users
			.FirstOrDefaultAsync(u => u.Id == userId && u.IsSuperAdmin && u.IsActive);

		if (superAdmin != null)
		{
			return new UserWithPermissions
			{
				Id = superAdmin.Id,
				Email = superAdmin.Email,
				IsActive = superAdmin.IsActive,
				IsSuperAdmin = true,
				Roles = new List<string> { "Super Admin" },
				Permissions = Permissions.All.ToList()
			};
		}

		// Regular tenant user
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
			IsSuperAdmin = false,
			Roles = roles,
			Permissions = permissions
		};
	}

	public async Task<AuthResult?> RefreshTokenAsync(string refreshToken)
	{
		// Find refresh token - could be Super Admin or tenant user
		var tokenEntity = await _context.RefreshTokens
			.Include(rt => rt.User)
				.ThenInclude(u => u.UserRoles)
					.ThenInclude(ur => ur.Role)
						.ThenInclude(r => r.RolePermissions)
							.ThenInclude(rp => rp.Permission)
			.FirstOrDefaultAsync(rt => rt.Token == refreshToken
				&& !rt.IsRevoked
				&& rt.ExpiresAt > DateTime.UtcNow);

		if (tokenEntity == null || tokenEntity.User == null || !tokenEntity.User.IsActive)
		{
			return null;
		}

		// For tenant users, verify tenant context matches
		if (!tokenEntity.User.IsSuperAdmin)
		{
			if (!_tenantContext.TenantId.HasValue || tokenEntity.TenantId != _tenantContext.TenantId.Value)
			{
				return null;
			}
		}

		// Revoke old refresh token
		tokenEntity.IsRevoked = true;

		List<string> permissions;
		if (tokenEntity.User.IsSuperAdmin)
		{
			permissions = Permissions.All.ToList();
		}
		else
		{
			permissions = tokenEntity.User.UserRoles
				.SelectMany(ur => ur.Role.RolePermissions)
				.Select(rp => rp.Permission.Key)
				.Distinct()
				.ToList();
		}

		var newToken = _jwtService.GenerateToken(
			tokenEntity.User.Id,
			tokenEntity.User.Email,
			tokenEntity.User.TenantId,
			permissions,
			tokenEntity.User.IsSuperAdmin);
		var newRefreshToken = GenerateRefreshToken();

		var newRefreshTokenEntity = new RefreshToken
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
			Permissions = permissions,
			IsSuperAdmin = tokenEntity.User.IsSuperAdmin
		};
	}

	public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
	{
		var tokenEntity = await _context.RefreshTokens
			.Include(rt => rt.User)
			.FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

		if (tokenEntity == null)
		{
			return false;
		}

		// For tenant users, verify tenant context matches
		if (tokenEntity.User != null && !tokenEntity.User.IsSuperAdmin)
		{
			if (!_tenantContext.TenantId.HasValue || tokenEntity.TenantId != _tenantContext.TenantId.Value)
			{
				return false;
			}
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
	public bool IsSuperAdmin { get; set; } = false;
}

public class UserWithPermissions
{
	public int Id { get; set; }
	public string Email { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public bool IsSuperAdmin { get; set; } = false;
	public List<string> Roles { get; set; } = new();
	public List<string> Permissions { get; set; } = new();
}
