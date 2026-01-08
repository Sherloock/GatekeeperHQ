using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Roles;

public class AddPermissionRequest
{
    [Required]
    public int PermissionId { get; set; }
}
