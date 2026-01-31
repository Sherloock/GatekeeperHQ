namespace GatekeeperHQ.Domain.Entities;

public class Invitation
{
	public int Id { get; set; }
	public int TenantId { get; set; }
	public string Email { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
	public int? RoleId { get; set; }
	public DateTime ExpiresAt { get; set; }
	public DateTime? AcceptedAt { get; set; }
	public int CreatedByUserId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Navigation properties
	public Tenant Tenant { get; set; } = null!;
	public Role? Role { get; set; }
	public User CreatedBy { get; set; } = null!;

	// Computed properties
	public bool IsExpired => DateTime.UtcNow > ExpiresAt;
	public bool IsAccepted => AcceptedAt.HasValue;
	public bool IsPending => !IsExpired && !IsAccepted;
}
