namespace AlMal.Application.DTOs.Auth;

public class RegisterRequest
{
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = null!;
}
