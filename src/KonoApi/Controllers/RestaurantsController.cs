using Kono.Restaurants.Domain;
using Kono.Infrastructure.Persistence;
using KonoInfrastructure.Contracts.Auth;
using KonoInfrastructure.Contracts.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kono.Infrastructure.Repositories.RestaurantRepository;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Restaurants.Services;
using Kono.Identity.Repositories;
using Kono.Infrastructure.Auth.Services;
using Kono.Infrastructure.Contracts.MenuItems;
using Kono.Menu.Domain;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RestaurantsController : ControllerBase
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly MainRestaurantServices _mainRestaurantServices;
    private readonly UserServices _userServices;

    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(30);


    public RestaurantsController(KonoDbContext context, 
                                IRestaurantRepository restaurantRepository, 
                                MainRestaurantServices mainRestaurantServices, 
                                UserServices userServices)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _mainRestaurantServices = mainRestaurantServices;
        _userServices = userServices;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurants = _mainRestaurantServices.GetRestaurant(ownerId);

        return Ok(restaurants);
    }

    [HttpGet("available-users")]
    public async Task<IActionResult> GetAvailableUsers()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var availableUsers = await _userServices.GetUnemployedUsersAsync();

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

        var availableUsers = await _mainRestaurantServices.GetSomeAvailableUsersAsync(query);

        return Ok(availableUsers);
    }


    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] CreateRestaurantInviteRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var inviteResult = await _mainRestaurantServices.CheckUserInvites(request.UserId, request.RestaurantId);

        if(!inviteResult.RestaurantSuccess){
            return BadRequest(new { message = inviteResult.Message });
        }

        if(!inviteResult.UserSuccess){
            return BadRequest(new { message = inviteResult.Message });
        }

        if(!inviteResult.PendingInviteSuccess){
            return BadRequest(new { message = inviteResult.Message });
        }

        var invite = await _mainRestaurantServices.GenerateInvite(InviteLifetime, request.UserId, request.RestaurantId);

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

        var invites = await _mainRestaurantServices.GetPendingInvitesForUser(userId);

        return Ok(invites);
    }

    [HttpGet("{restaurantId:guid}/invites")]
    public async Task<IActionResult> GetRestaurantInvites(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var invites = await _mainRestaurantServices.GetPendingInvitesForOwner(ownerId, restaurantId);

        return Ok(invites);
    }

    [HttpPatch("invite/{inviteId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid inviteId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var result = await _mainRestaurantServices.AcceptInvite(inviteId, userId);

        return result.Error switch
        {
            MembershipError.None => Ok(result.Response),
            MembershipError.NotFound => NotFound(new { message = result.Message }),
            MembershipError.Forbidden => Forbid(),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpPatch("kick/{userId:guid}")]
    public async Task<IActionResult> KickUser(Guid userId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var result = await _mainRestaurantServices.KickUser(userId, ownerId);

        return result.Error switch
        {
            MembershipError.None => Ok(result.Response),
            MembershipError.NotFound => NotFound(new { message = result.Message }),
            MembershipError.Forbidden => Forbid(),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpPost("{restaurantId:guid}/add-food-item")]
    public async Task<IActionResult> AddFoodItem(Guid restaurantId, [FromBody] CreateMenuItemRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
        {
            return Forbid();
        }

        var response = await _mainRestaurantServices.AddFoodItem(restaurantId, request);

        return Ok(response);
    }
}
