namespace Kono.Identity.Domain.Owners.Login;

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public Guid? UserId { get; set; }
}
