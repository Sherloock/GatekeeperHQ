using System.ComponentModel.DataAnnotations;

namespace GatekeeperHQ.API.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(1, ErrorMessage = "Password cannot be empty")]
    public string Password { get; set; } = string.Empty;
}
