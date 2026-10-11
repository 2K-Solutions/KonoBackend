namespace KonoInfrastructure.Contracts.Auth;

public sealed record RegisterOwnerRequest(
    string Email,
    string Password,
    string? FirstName,
    string? SecondName,
    string? PhoneNumber);
