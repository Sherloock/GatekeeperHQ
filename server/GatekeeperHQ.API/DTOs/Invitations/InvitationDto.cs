namespace GatekeeperHQ.API.DTOs.Invitations;

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

public class AcceptInvitationRequest
{
	public string Password { get; set; } = string.Empty;
}

public class AcceptInvitationResponse
{
	public bool Success { get; set; }
	public string? Error { get; set; }
	public int? UserId { get; set; }
	public string? Email { get; set; }
	public int? TenantId { get; set; }
	public string? TenantName { get; set; }
}

public class ValidateInvitationResponse
{
	public bool Valid { get; set; }
	public string? Email { get; set; }
	public string? TenantName { get; set; }
	public string? RoleName { get; set; }
	public DateTime? ExpiresAt { get; set; }
	public string? Error { get; set; }
}
