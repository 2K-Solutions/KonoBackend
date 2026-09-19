using Kono.Identity.Domain.Restaurants;
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

    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(10);

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] CreateRestaurantInviteRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == request.RestaurantId && r.OwnerId == ownerId && r.DeletedAt == null);
        if (restaurant is null) return NotFound(new { message = "Restaurant not found" });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = "User not found" });
        if (user.RestaurantId != null) return BadRequest(new { message = "User already belongs to a restaurant" });

        var now = DateTime.UtcNow;

        var existingPendingInvite = await _context.RestaurantInvites
            .FirstOrDefaultAsync(i => i.UserId == request.UserId
                && i.Status == RestaurantInviteStatus.Pending
                && i.ExpiresAt > now);
        if (existingPendingInvite != null) return BadRequest(new { message = "User already has a pending invite" });

        var invite = new RestaurantInvite
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            OwnerId = ownerId,
            UserId = request.UserId,
            Status = RestaurantInviteStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(InviteLifetime)
        };

        _context.RestaurantInvites.Add(invite);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            invite.Id,
            invite.RestaurantId,
            invite.UserId,
            invite.ExpiresAt
        });
    }

    [HttpGet("invites/mine")]
    public async Task<IActionResult> GetMyInvites()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var now = DateTime.UtcNow;

        var invites = await _context.RestaurantInvites
            .Where(i => i.UserId == userId && i.Status == RestaurantInviteStatus.Pending && i.ExpiresAt > now)
            .Join(_context.Restaurants, i => i.RestaurantId, r => r.Id, (i, r) => new
            {
                i.Id,
                i.RestaurantId,
                RestaurantName = r.RestaurantName,
                i.ExpiresAt
            })
            .ToListAsync();

        return Ok(invites);
    }

    [HttpPatch("invite/{inviteId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid inviteId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var invite = await _context.RestaurantInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.UserId == userId);
        if (invite is null) return NotFound(new { message = "Invite not found" });

        if (invite.Status != RestaurantInviteStatus.Pending)
            return BadRequest(new { message = "Invite is no longer pending" });

        var now = DateTime.UtcNow;
        if (invite.ExpiresAt <= now)
        {
            invite.Status = RestaurantInviteStatus.Expired;
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "Invite has expired" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = "User not found" });
        if (user.RestaurantId != null) return BadRequest(new { message = "User already belongs to a restaurant" });

        user.RestaurantId = invite.RestaurantId;
        invite.Status = RestaurantInviteStatus.Accepted;
        invite.RespondedAt = now;

        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.RestaurantId });
    }

    [HttpPatch("kick/{userId:guid}")]
    public async Task<IActionResult> KickUser(Guid userId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = "User not found" });
        if (user.RestaurantId is null) return BadRequest(new { message = "User does not belong to a restaurant" });

        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == user.RestaurantId && r.OwnerId == ownerId && r.DeletedAt == null);
        if (restaurant is null) return Forbid();

        user.RestaurantId = null;
        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.RestaurantId });
    }
}

public class CreateRestaurantInviteRequest
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
}

public class BasicUserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
}
