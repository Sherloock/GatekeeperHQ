namespace GatekeeperHQ.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int TenantId { get; set; }
    public string Action { get; set; } = string.Empty; // create, update, delete, login, etc.
    public string EntityType { get; set; } = string.Empty; // User, Role, Permission, etc.
    public int? EntityId { get; set; }
    public string? Changes { get; set; } // JSON of changes
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
