namespace KonoApi.Contracts.Auth;

public sealed record TokenResponse(string AccessToken, string RefreshToken, Guid UserId, string Message);
