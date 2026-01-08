using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Users;

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }

    public bool? IsActive { get; set; }

    public List<int>? RoleIds { get; set; }
}
