using Kono.Identity.Domain.Users.Login;
using Kono.Infrastructure.Persistence;
using Kono.Infrastructure.Auth.Services;
using KonoInfrastructure.Contracts.Auth;
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
    // Login just for the user 
    [HttpPost("user/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        var result = await _authService.LoginUserAsync(request.Email, request.Password);
        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(ToTokenResponse(result));
    }
    //Login for the owner 
    [HttpPost("owner/login")]
    public async Task<IActionResult> OwnerLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        var result = await _authService.LoginOwnerAsync(request.Email, request.Password);
        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(ToTokenResponse(result));
    }
    // Register just for the user
    [HttpPost("user/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        if(request.Password.Length < 8){
            return BadRequest(new { message = "Password must be at least 8 characters long" });
        }

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

        return Ok(ToTokenResponse(result));
    }
    // Register for the owner, only admin can register the owner
    [HttpPost("owner/register")]
    [Authorize]
    public async Task<IActionResult> OwnerRegister([FromBody] RegisterOwnerRequest request)
    {
        var isAdmin = User.FindFirst("isAdmin")?.Value;
        if (isAdmin != "true") return Forbid();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        if(request.Password.Length < 8){
            return BadRequest(new { message = "Password must be at least 8 characters long" });
        }

        var result = await _authService.RegisterOwnerAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.SecondName,
            request.PhoneNumber);

        if (!result.Success) return BadRequest(new { message = result.Message });

        return Ok(new { ownerId = result.UserId, message = result.Message });
    }
    // Refresh the user's or the owners refresh token
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var result = await _authService.RefreshUserTokenAsync(request.RefreshToken);
        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(ToTokenResponse(result));
    }
    //Validates the refresh token for the user or the owner
    [HttpPost("validate-refresh")]
    public async Task<IActionResult> ValidateRefresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var valid = await _authService.ValidateRefreshTokenAsync(request.RefreshToken);
        if (!valid) return Unauthorized(new { message = "Refresh token is invalid or expired" });

        return Ok(new { message = "Refresh token is valid" });
    }
    //Validates the user return user info
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
        });
    }
    // Logout the user or the owner by revoking the refresh token
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

        return Ok(new { message = "Logged out successfully" });
    }

    private static TokenResponse ToTokenResponse(LoginResult result) =>
        new(result.AccessToken ?? string.Empty, result.RefreshToken ?? string.Empty, result.UserId ?? Guid.Empty, result.Message);
}

