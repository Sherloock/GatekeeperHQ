namespace GatekeeperHQ.API.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
