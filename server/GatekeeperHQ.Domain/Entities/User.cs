namespace GatekeeperHQ.Domain.Entities;

public class User
{
	public int Id { get; set; }
	public int? TenantId { get; set; }  // Nullable for Super Admin
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public bool IsActive { get; set; } = true;
	public bool IsSuperAdmin { get; set; } = false;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	// Navigation properties
	public Tenant? Tenant { get; set; }
	public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
