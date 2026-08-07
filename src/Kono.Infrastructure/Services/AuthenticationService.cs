using Kono.Infrastructure.Persistence;
using Kono.Infrastructure.Repositories;
using Kono.Identity.Domain.Users;
using Kono.Identity.Domain.Users.Login;
using Kono.Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Services;

public interface IAuthenticationService
{
    Task<LoginResult> LoginUserAsync(string email, string password);
    Task<LoginResult> LoginOwnerAsync(string email, string password);
    Task<LoginResult> RegisterUserAsync(string email, string password, string username,
        string? firstName, string? secondName, string? phoneNumber, UserRole? userRole, string? mobilePhoneType);
    Task<LoginResult> RefreshUserTokenAsync(string refreshToken);
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

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, "worker");
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

        var accessToken = _jwtTokenService.GenerateAccessToken(newUser.Id, newUser.Email, "worker");
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

        var user = await _userRepository.GetByIdAsync(storedRefreshToken.UserId);

        if (user == null)
        {
            return new LoginResult
            {
                Success = false,
                Message = "User not found"
            };
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, "worker");
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
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

        var userExists = await _userRepository.GetByIdAsync(storedRefreshToken.UserId) != null;
        return userExists;
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
