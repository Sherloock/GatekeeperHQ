using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Users;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> RoleIds { get; set; } = new();
}
