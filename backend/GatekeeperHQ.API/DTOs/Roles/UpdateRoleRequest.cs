using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Roles;

public class UpdateRoleRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public List<int>? PermissionIds { get; set; }
}
