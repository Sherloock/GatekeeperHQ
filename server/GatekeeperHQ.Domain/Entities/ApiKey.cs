namespace GatekeeperHQ.Domain.Entities;

public class ApiKey
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty; // Hashed version for storage
    public string Permissions { get; set; } = string.Empty; // JSON array of permissions
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
