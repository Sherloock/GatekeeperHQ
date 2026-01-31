namespace GatekeeperHQ.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
