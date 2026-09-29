using Kono.Identity.Domain.Users;

namespace KonoApi.Contracts.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string Username,
    string? FirstName,
    string? SecondName,
    string? PhoneNumber,
    UserRole? UserRole,
    string? MobilePhoneType);
