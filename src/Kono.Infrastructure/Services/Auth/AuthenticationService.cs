using Kono.Infrastructure.Persistence;
using Kono.Identity.Repositories;
using Kono.Identity.Domain.Users;
using Kono.Identity.Domain.Users.Login;
using Kono.Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Auth.Services;

public interface IAuthenticationService
{
    Task<LoginResult> LoginUserAsync(string email, string password);
    Task<LoginResult> LoginOwnerAsync(string email, string password);
    Task<LoginResult> RegisterUserAsync(string email, string password, string username,
        string? firstName, string? secondName, string? phoneNumber, UserRole? userRole, string? mobilePhoneType);
    Task<LoginResult> RegisterOwnerAsync(string email, string password,
        string? firstName, string? secondName, string? phoneNumber);
    Task<LoginResult> RefreshUserTokenAsync(string refreshToken);
    Task<LoginResult> IssueUserTokensAsync(Guid userId);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IOwnerRepository _ownerRepository;
    private readonly IRefreshTokenRepository _refreshRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(
        IUserRepository userRepository,
        IOwnerRepository ownerRepository,
        IRefreshTokenRepository refreshRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _ownerRepository = ownerRepository;
        _refreshRepository = refreshRepository;
        _jwtTokenService = jwtTokenService;
    }

    // Users without a restaurant are "unemployed"; once they join one they become "worker".
    private static string GetUserAccountType(User user)
        => user.RestaurantId == null ? "unemployed" : "worker";

    public async Task<LoginResult> LoginUserAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, GetUserAccountType(user));
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiryDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenExpirationDays()), DateTimeKind.Unspecified),
            IsRevoked = false,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _refreshRepository.AddAsync(refreshTokenEntity);
        await _refreshRepository.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id
        };
    }

    public async Task<LoginResult> LoginOwnerAsync(string email, string password)
    {
        var owner = await _ownerRepository.GetByEmailAsync(email);

        if (owner == null || !BCrypt.Net.BCrypt.Verify(password, owner.Password))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(owner.Id, owner.Email, "owner");
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            Token = refreshToken,
            ExpiryDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenExpirationDays()), DateTimeKind.Unspecified),
            IsRevoked = false,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _refreshRepository.AddAsync(refreshTokenEntity);
        await _refreshRepository.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = owner.Id
        };
    }

    public async Task<LoginResult> RegisterUserAsync(string email, string password, string username,
        string? firstName, string? secondName, string? phoneNumber, UserRole? userRole, string? mobilePhoneType)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new LoginResult { Success = false, Message = "Email and password are required" };
        }

        if (!email.Contains('@') || email.Length < 5)
        {
            return new LoginResult { Success = false, Message = "Invalid email format" };
        }

        if (password.Length < 8)
        {
            return new LoginResult { Success = false, Message = "Password must be at least 8 characters" };
        }

        var existing = await _userRepository.ExistsByEmailAsync(email);
        if (existing)
        {
            return new LoginResult { Success = false, Message = "Email already registered" };
        }

        var hashed = BCrypt.Net.BCrypt.HashPassword(password);

        var newUser = new Kono.Identity.Domain.Users.User
        {
            Id = Guid.NewGuid(),
            RestaurantId = null,
            Email = email,
            Password = hashed,
            Username = username ?? string.Empty,
            FirstName = firstName ?? string.Empty,
            SecondName = secondName ?? string.Empty,
            PhoneNumber = phoneNumber ?? string.Empty,
            UserRole = userRole,
            MobilePhoneType = mobilePhoneType,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _userRepository.AddAsync(newUser);

        var accessToken = _jwtTokenService.GenerateAccessToken(newUser.Id, newUser.Email, GetUserAccountType(newUser));
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new Kono.Identity.Domain.RefreshTokens.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            Token = refreshToken,
            ExpiryDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenExpirationDays()), DateTimeKind.Unspecified),
            IsRevoked = false,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _refreshRepository.AddAsync(refreshTokenEntity);
        await _userRepository.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = newUser.Id
        };
    }

    public async Task<LoginResult> RegisterOwnerAsync(string email, string password,
        string? firstName, string? secondName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new LoginResult { Success = false, Message = "Email and password are required" };
        }

        email = email.Trim();

        if (!email.Contains('@') || email.Length < 5)
        {
            return new LoginResult { Success = false, Message = "Invalid email format" };
        }

        if (password.Length < 8)
        {
            return new LoginResult { Success = false, Message = "Password must be at least 8 characters" };
        }

        if (await _ownerRepository.ExistsByEmailAsync(email))
        {
            return new LoginResult { Success = false, Message = "Email already registered" };
        }

        var newOwner = new Kono.Identity.Domain.Owners.Owner
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = firstName ?? string.Empty,
            SecondName = secondName ?? string.Empty,
            PhoneNumber = phoneNumber ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _ownerRepository.AddAsync(newOwner);
        await _ownerRepository.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Message = "Owner registered successfully",
            UserId = newOwner.Id
        };
    }

    public async Task<LoginResult> RefreshUserTokenAsync(string refreshToken)
    {
        var storedRefreshToken = await _refreshRepository.GetByTokenAsync(refreshToken);
        if (storedRefreshToken == null || storedRefreshToken.IsRevoked || storedRefreshToken.ExpiryDate <= DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            };
        }

        var account = await FindAccountAsync(storedRefreshToken.UserId);

        if (account == null)
        {
            return new LoginResult
            {
                Success = false,
                Message = "User not found"
            };
        }

        var (accountId, email, accountType) = account.Value;

        var newAccessToken = _jwtTokenService.GenerateAccessToken(accountId, email, accountType);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = accountId,
            Token = newRefreshToken,
            ExpiryDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenExpirationDays()), DateTimeKind.Unspecified),
            IsRevoked = false,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _refreshRepository.UpdateAsync(storedRefreshToken);
        await _refreshRepository.AddAsync(newRefreshTokenEntity);
        await _refreshRepository.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            UserId = accountId
        };
    }

    private async Task<(Guid Id, string Email, string AccountType)?> FindAccountAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null) return (user.Id, user.Email, GetUserAccountType(user));

        var owner = await _ownerRepository.GetByIdAsync(id);
        if (owner != null) return (owner.Id, owner.Email, "owner");

        return null;
    }

    public async Task<LoginResult> IssueUserTokensAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return new LoginResult
            {
                Success = false,
                Message = "User not found"
            };
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, GetUserAccountType(user));
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiryDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenExpirationDays()), DateTimeKind.Unspecified),
            IsRevoked = false,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _refreshRepository.AddAsync(refreshTokenEntity);
        await _refreshRepository.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Message = "Tokens issued",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id
        };
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        var storedRefreshToken = await _refreshRepository.GetByTokenAsync(refreshToken);
        if (storedRefreshToken == null || storedRefreshToken.IsRevoked || storedRefreshToken.ExpiryDate <= DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified))
        {
            return false;
        }

        return await FindAccountAsync(storedRefreshToken.UserId) != null;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var storedRefreshToken = await _refreshRepository.GetByTokenAsync(refreshToken);
        if (storedRefreshToken != null)
        {
            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            await _refreshRepository.UpdateAsync(storedRefreshToken);
            await _refreshRepository.SaveChangesAsync();
        }
    }

}
