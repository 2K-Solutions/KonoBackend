namespace KonoApi.Contracts.Auth;

public sealed record BasicUserInfo(Guid Id, string Email, string Username, string FirstName, string SecondName);
