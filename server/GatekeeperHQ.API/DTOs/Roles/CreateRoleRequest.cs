using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Roles;

public class CreateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new();
}
