namespace Kono.Identity.Domain.Users.Login;

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public Guid? UserId { get; set; }
}
