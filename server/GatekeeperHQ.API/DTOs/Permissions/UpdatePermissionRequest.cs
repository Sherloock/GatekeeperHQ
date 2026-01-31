using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Permissions;

public class UpdatePermissionRequest
{
    [StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$", ErrorMessage = "Key must be in format 'resource.action' (e.g., 'reports.view')")]
    public string? Key { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
