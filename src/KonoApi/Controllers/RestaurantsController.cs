using Kono.Restaurants.Domain;
using Kono.Infrastructure.Persistence;
using KonoInfrastructure.Contracts.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Restaurants.Services;
using Kono.Infrastructure.Auth.Services;
using Kono.Infrastructure.Contracts.Restaurants;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RestaurantsController : ControllerBase
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ITableRepository _tableRepository;
    private readonly MainRestaurantServices _mainRestaurantServices;
    private readonly UserServices _userServices;

    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(30);


    public RestaurantsController(KonoDbContext context, 
                                IRestaurantRepository restaurantRepository, 
                                ITableRepository tableRepository,
                                MainRestaurantServices mainRestaurantServices, 
                                UserServices userServices)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _tableRepository = tableRepository;
        _mainRestaurantServices = mainRestaurantServices;
        _userServices = userServices;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRestaurant([FromBody] CreateRestaurantRequest request)
    {
        var isAdmin = User.FindFirst("isAdmin")?.Value;
        if (isAdmin != "true") return Forbid();

        if (string.IsNullOrWhiteSpace(request.RestaurantName) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest(new { message = "Restaurant name, city and address are required" });
        }

        var restaurant = await _mainRestaurantServices.CreateRestaurant(request);
        if (restaurant is null) return NotFound(new { message = "Owner not found" });

        return Ok(restaurant);
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

    [HttpGet("available-users/search")] //Retrieves a list of 5 users with no restaurantId
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

    [HttpPatch("invite/{inviteId:guid}/decline")]
    public async Task<IActionResult> DeclineInvite(Guid inviteId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var result = await _mainRestaurantServices.DeclineInvite(inviteId, userId);

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

    [HttpGet("{restaurantId:guid}/get-my-workers")]
    public async Task<IActionResult> GetMyWorkers(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var workers = await _mainRestaurantServices.GetRestaurantUsers(restaurantId, ownerId);

        return Ok(workers);
    }

    [HttpPost("{restaurantId:guid}/tables/add-table")]
    public async Task<IActionResult> AddTable(Guid restaurantId, [FromBody] AddTableRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var table = await _mainRestaurantServices.AddTable(restaurantId, request, ownerId);

        return Ok(table);
    }

    [HttpPatch("{restaurantId:guid}/tables/{tableId:guid}/edit-table")]
    public async Task<IActionResult> EditTable(Guid restaurantId, Guid tableId, [FromBody] EditTableRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var table = await _mainRestaurantServices.EditTableNumber(restaurantId, tableId, request, ownerId);

        return Ok(table);
    }

    [HttpGet("{restaurantId:guid}/tables")]
    public async Task<IActionResult> GetTables(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var tables = await _mainRestaurantServices.GetTables(restaurantId, ownerId);

        return Ok(tables);
    }
}
