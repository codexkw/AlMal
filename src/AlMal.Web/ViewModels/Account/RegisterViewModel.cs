namespace AlMal.Web.ViewModels.Account;

public class RegisterViewModel
{
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
