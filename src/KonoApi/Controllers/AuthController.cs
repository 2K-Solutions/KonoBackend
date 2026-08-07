using Kono.Identity.Domain.Users;
using Kono.Infrastructure.Persistence;
using Kono.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly KonoDbContext _context;

    public AuthController(IAuthenticationService authService, KonoDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        var result = await _authService.LoginUserAsync(request.Email, request.Password);
        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("owner/login")]
    public async Task<IActionResult> OwnerLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        var result = await _authService.LoginOwnerAsync(request.Email, request.Password);
        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username is required" });

        var result = await _authService.RegisterUserAsync(
            request.Email,
            request.Password,
            request.Username,
            request.FirstName,
            request.SecondName,
            request.PhoneNumber,
            request.UserRole,
            request.MobilePhoneType);

        if (!result.Success) return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var result = await _authService.RefreshUserTokenAsync(request.RefreshToken);
        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("validate-refresh")]
    public async Task<IActionResult> ValidateRefresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var valid = await _authService.ValidateRefreshTokenAsync(request.RefreshToken);
        if (!valid) return Unauthorized(new { message = "Refresh token is invalid or expired" });

        return Ok(new { message = "Refresh token is valid" });
    }

    [HttpGet("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateAccess()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (currentUser == null) return Unauthorized();

        return Ok(new
        {
            currentUser.Id,
            currentUser.Email,
            currentUser.Username,
            currentUser.FirstName,
            currentUser.SecondName,
            currentUser.PhoneNumber,
            currentUser.UserRole,
            currentUser.MobilePhoneType
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

        return Ok(new { message = "Logged out successfully" });
    }
}

// Request/Response DTOs
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole? UserRole { get; set; }
    public string MobilePhoneType { get; set; } = string.Empty;
}
