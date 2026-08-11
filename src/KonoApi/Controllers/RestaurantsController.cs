using Kono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RestaurantsController : ControllerBase
{
    private readonly KonoDbContext _context;

    public RestaurantsController(KonoDbContext context)
    {
        _context = context;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurants = await _context.Restaurants
            .Where(r => r.OwnerId == ownerId && r.DeletedAt == null)
            .Select(r => new { r.Id, r.RestaurantName })
            .ToListAsync();

        return Ok(restaurants);
    }

    [HttpGet("available-users")]
    public async Task<IActionResult> GetAvailableUsers()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var availableUsers = await _context.Users
            .Where(u => u.RestaurantId == null && u.DeletedAt == null)
            .Select(u => new BasicUserInfo
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
                FirstName = u.FirstName,
                SecondName = u.SecondName
            })
            .ToListAsync();

        return Ok(availableUsers);
    }

    [HttpGet("available-users/search")]
    public async Task<IActionResult> SearchAvailableUsers([FromQuery] string query)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        query = query.Trim();

        var availableUsers = await _context.Users
            .Where(u => u.RestaurantId == null && u.DeletedAt == null)
            .Where(u => EF.Functions.ILike(u.Username, $"%{query}%"))
            .OrderBy(u => u.Username)
            .Select(u => new BasicUserInfo
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
                FirstName = u.FirstName,
                SecondName = u.SecondName
            })
            .Take(5)
            .ToListAsync();

        return Ok(availableUsers);
    }
}

public class BasicUserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
}
