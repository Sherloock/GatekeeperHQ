using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Users;

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [MaxLength(128, ErrorMessage = "Password must be less than 128 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string? Password { get; set; }

    public bool? IsActive { get; set; }

    public List<int>? RoleIds { get; set; }
}
