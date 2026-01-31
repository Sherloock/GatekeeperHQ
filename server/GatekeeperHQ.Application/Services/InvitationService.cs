using System.Security.Cryptography;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IInvitationService
{
	Task<InvitationDto> CreateInvitationAsync(int tenantId, CreateInvitationRequest request, int createdByUserId);
	Task<InvitationDto?> GetInvitationByTokenAsync(string token);
	Task<AcceptInvitationResult> AcceptInvitationAsync(string token, string password);
	Task<bool> RevokeInvitationAsync(int id);
	Task<List<InvitationDto>> GetTenantInvitationsAsync(int tenantId);
}

public class InvitationService : IInvitationService
{
	private readonly AppDbContext _context;
	private readonly int _invitationExpirationDays = 7;

	public InvitationService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<InvitationDto> CreateInvitationAsync(int tenantId, CreateInvitationRequest request, int createdByUserId)
	{
		// Verify tenant exists
		var tenant = await _context.Tenants.FindAsync(tenantId);
		if (tenant == null)
		{
			throw new InvalidOperationException("Tenant not found");
		}

		// Check if email already has a pending invitation for this tenant
		var existingInvitation = await _context.Invitations
			.FirstOrDefaultAsync(i => i.TenantId == tenantId
				&& i.Email == request.Email
				&& i.AcceptedAt == null
				&& i.ExpiresAt > DateTime.UtcNow);

		if (existingInvitation != null)
		{
			throw new InvalidOperationException("An invitation for this email already exists");
		}

		// Check if user with this email already exists in this tenant
		var existingUser = await _context.Users
			.FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenantId);

		if (existingUser != null)
		{
			throw new InvalidOperationException("A user with this email already exists in this tenant");
		}

		// Validate role if provided
		if (request.RoleId.HasValue)
		{
			var role = await _context.Roles
				.FirstOrDefaultAsync(r => r.Id == request.RoleId.Value && r.TenantId == tenantId);

			if (role == null)
			{
				throw new InvalidOperationException("Invalid role for this tenant");
			}
		}

		var invitation = new Invitation
		{
			TenantId = tenantId,
			Email = request.Email.ToLower(),
			Token = GenerateInvitationToken(),
			RoleId = request.RoleId,
			ExpiresAt = DateTime.UtcNow.AddDays(_invitationExpirationDays),
			CreatedByUserId = createdByUserId,
			CreatedAt = DateTime.UtcNow
		};

		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		return await GetInvitationDtoAsync(invitation.Id)
			?? throw new InvalidOperationException("Failed to create invitation");
	}

	public async Task<InvitationDto?> GetInvitationByTokenAsync(string token)
	{
		var invitation = await _context.Invitations
			.Include(i => i.Tenant)
			.Include(i => i.Role)
			.FirstOrDefaultAsync(i => i.Token == token);

		if (invitation == null)
			return null;

		return MapToDto(invitation);
	}

	public async Task<AcceptInvitationResult> AcceptInvitationAsync(string token, string password)
	{
		var invitation = await _context.Invitations
			.Include(i => i.Tenant)
			.FirstOrDefaultAsync(i => i.Token == token);

		if (invitation == null)
		{
			return new AcceptInvitationResult { Success = false, Error = "Invalid invitation token" };
		}

		if (invitation.IsExpired)
		{
			return new AcceptInvitationResult { Success = false, Error = "Invitation has expired" };
		}

		if (invitation.IsAccepted)
		{
			return new AcceptInvitationResult { Success = false, Error = "Invitation has already been accepted" };
		}

		// Check if email is already taken (globally now, since email is unique)
		var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == invitation.Email);
		if (existingUser != null)
		{
			return new AcceptInvitationResult { Success = false, Error = "A user with this email already exists" };
		}

		// Create the user
		var user = new User
		{
			TenantId = invitation.TenantId,
			Email = invitation.Email,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
			IsActive = true,
			IsSuperAdmin = false,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		// Assign role if specified
		if (invitation.RoleId.HasValue)
		{
			var userRole = new UserRole
			{
				UserId = user.Id,
				RoleId = invitation.RoleId.Value
			};
			_context.UserRoles.Add(userRole);
		}

		// Mark invitation as accepted
		invitation.AcceptedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return new AcceptInvitationResult
		{
			Success = true,
			UserId = user.Id,
			Email = user.Email,
			TenantId = invitation.TenantId,
			TenantName = invitation.Tenant.Name
		};
	}

	public async Task<bool> RevokeInvitationAsync(int id)
	{
		var invitation = await _context.Invitations.FindAsync(id);
		if (invitation == null)
			return false;

		// Can't revoke already accepted invitations
		if (invitation.IsAccepted)
		{
			throw new InvalidOperationException("Cannot revoke an invitation that has already been accepted");
		}

		_context.Invitations.Remove(invitation);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<List<InvitationDto>> GetTenantInvitationsAsync(int tenantId)
	{
		var invitations = await _context.Invitations
			.Include(i => i.Tenant)
			.Include(i => i.Role)
			.Include(i => i.CreatedBy)
			.Where(i => i.TenantId == tenantId)
			.OrderByDescending(i => i.CreatedAt)
			.ToListAsync();

		return invitations.Select(MapToDto).ToList();
	}

	private async Task<InvitationDto?> GetInvitationDtoAsync(int id)
	{
		var invitation = await _context.Invitations
			.Include(i => i.Tenant)
			.Include(i => i.Role)
			.Include(i => i.CreatedBy)
			.FirstOrDefaultAsync(i => i.Id == id);

		return invitation == null ? null : MapToDto(invitation);
	}

	private static InvitationDto MapToDto(Invitation invitation)
	{
		return new InvitationDto
		{
			Id = invitation.Id,
			TenantId = invitation.TenantId,
			TenantName = invitation.Tenant?.Name ?? string.Empty,
			Email = invitation.Email,
			Token = invitation.Token,
			RoleId = invitation.RoleId,
			RoleName = invitation.Role?.Name,
			ExpiresAt = invitation.ExpiresAt,
			AcceptedAt = invitation.AcceptedAt,
			CreatedByUserId = invitation.CreatedByUserId,
			CreatedByEmail = invitation.CreatedBy?.Email ?? string.Empty,
			CreatedAt = invitation.CreatedAt,
			Status = invitation.IsAccepted ? "accepted" : invitation.IsExpired ? "expired" : "pending"
		};
	}

	private static string GenerateInvitationToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		return Convert.ToBase64String(bytes)
			.Replace("+", "-")
			.Replace("/", "_")
			.Replace("=", "");
	}
}

// DTOs
public class InvitationDto
{
	public int Id { get; set; }
	public int TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
	public int? RoleId { get; set; }
	public string? RoleName { get; set; }
	public DateTime ExpiresAt { get; set; }
	public DateTime? AcceptedAt { get; set; }
	public int CreatedByUserId { get; set; }
	public string CreatedByEmail { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public string Status { get; set; } = string.Empty;
}

public class CreateInvitationRequest
{
	public string Email { get; set; } = string.Empty;
	public int? RoleId { get; set; }
}

public class AcceptInvitationResult
{
	public bool Success { get; set; }
	public string? Error { get; set; }
	public int? UserId { get; set; }
	public string? Email { get; set; }
	public int? TenantId { get; set; }
	public string? TenantName { get; set; }
}
