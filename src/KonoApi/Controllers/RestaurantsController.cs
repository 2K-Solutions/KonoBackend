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
using Kono.Infrastructure.Services.Notifications;

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
    private readonly IAuthenticationService _authService;
    private readonly INotificationService _notificationService;

    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(30);


    public RestaurantsController(KonoDbContext context,
                                IRestaurantRepository restaurantRepository,
                                ITableRepository tableRepository,
                                MainRestaurantServices mainRestaurantServices,
                                UserServices userServices,
                                IAuthenticationService authService,
                                INotificationService notificationService)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _tableRepository = tableRepository;
        _mainRestaurantServices = mainRestaurantServices;
        _userServices = userServices;
        _authService = authService;
        _notificationService = notificationService;
    }

    //Allows the admin to create a new restaurant and assign an owner to it
    [HttpPost("create-restaurant")]
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

    //Allows the owner to get all their restaurants
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurants = await _mainRestaurantServices.GetRestaurant(ownerId);

        return Ok(restaurants);
    }

    //Allows the owner to get a list of all users who are not currently employed by any restaurant
    [HttpGet("available-users")]
    public async Task<IActionResult> GetAvailableUsers()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var availableUsers = await _userServices.GetUnemployedUsersAsync();

        return Ok(availableUsers);
    }

    //Retrieves a list of 5 users with no restaurantId
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

    //Owner invites a user to join their restaurant. The invite is valid for 30 minutes.
    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] CreateRestaurantInviteRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var inviteResult = await _mainRestaurantServices.CheckUserInvites(request.UserId, request.RestaurantId, ownerId);

        if(!inviteResult.RestaurantSuccess){
            return BadRequest(new { message = inviteResult.Message });
        }

        if(!inviteResult.UserSuccess){
            return BadRequest(new { message = inviteResult.Message });
        }

        if(!inviteResult.PendingInviteSuccess){
            return BadRequest(new { message = inviteResult.Message });
        }

        var invite = await _mainRestaurantServices.GenerateInvite(InviteLifetime, request.UserId, request.RestaurantId, ownerId);

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(request.RestaurantId);
        await _notificationService.SendToUserAsync(
            request.UserId,
            "New restaurant invite",
            $"You've been invited to join {restaurant?.RestaurantName ?? "a restaurant"}. The invite expires in {InviteLifetime.TotalMinutes:0} minutes.",
            new Dictionary<string, string>
            {
                ["type"] = "restaurant_invite",
                ["inviteId"] = invite.Id.ToString(),
                ["restaurantId"] = invite.RestaurantId.ToString(),
                ["expiresAt"] = invite.ExpiresAt.ToString("o")
            });

        return Ok(new
        {
            invite.Id,
            invite.RestaurantId,
            invite.UserId,
            invite.ExpiresAt
        });
    }

    //Allows the user to get all their pending invites
    [HttpGet("invites/mine")]
    public async Task<IActionResult> GetMyInvites()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "unemployed") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var invites = await _mainRestaurantServices.GetPendingInvitesForUser(userId);

        return Ok(invites);
    }
    //Allows the owner to get all pending invites for a specific restaurant
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

    [HttpPatch("abandon-restaurant")]
    public async Task<IActionResult> AbandonRestaurant()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByUserIdAsync(userId);
        if (restaurant == null) return NotFound();
        Guid restaurantId = (Guid)restaurant;
        var result = await _mainRestaurantServices.AbandonRestaurant(userId, restaurantId);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        // Reissue tokens so the restaurantId claim is removed and accountType becomes "unemployed"
        var tokens = await _authService.IssueUserTokensAsync(userId);
        if (!tokens.Success) return NotFound(new { message = tokens.Message });

        result.AccessToken = tokens.AccessToken ?? string.Empty;
        result.RefreshToken = tokens.RefreshToken ?? string.Empty;

        return Ok(result);
    }

    //User accepts the invite
    [HttpPatch("invite/{inviteId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid inviteId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "unemployed") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var result = await _mainRestaurantServices.AcceptInvite(inviteId, userId);

        if (result.Error != MembershipError.None)
        {
            return result.Error switch
            {
                MembershipError.NotFound => NotFound(new { message = result.Message }),
                MembershipError.Forbidden => Forbid(),
                _ => BadRequest(new { message = result.Message })
            };
        }

        var tokens = await _authService.IssueUserTokensAsync(userId);
        if (!tokens.Success) return NotFound(new { message = tokens.Message });

        return Ok(new AcceptInviteResponse(
            result.Response!.Id,
            result.Response.RestaurantId,
            tokens.AccessToken ?? string.Empty,
            tokens.RefreshToken ?? string.Empty));
    }

    //User declines the invite
    [HttpPatch("invite/{inviteId:guid}/decline")]
    public async Task<IActionResult> DeclineInvite(Guid inviteId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "unemployed") return Forbid();

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

    //Allows the owner to kick a user from their restaurant
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

    //Allows the owner to get a list of all users who are currently employed by their restaurant
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

    //Allows the owner to add a new table to their restaurant
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

    //Allows the owner to edit a table in their restaurant
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

    //Allows the owner or worker to get current tables
    [HttpGet("{restaurantId:guid}/tables")]
    public async Task<IActionResult> GetTables()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner" && accountType != "worker") return Forbid();

        var restaurantIdClaim = User.FindFirst("restaurantId")?.Value;
        if (!Guid.TryParse(restaurantIdClaim, out var restaurantId)) return Unauthorized();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var tables = await _mainRestaurantServices.GetTables(restaurantId, userId);

        return Ok(tables);
    }

    //Allows the owner to delete a table from their restaurant
    [HttpDelete("{restaurantId:guid}/tables/{tableId:guid}/delete-table")]
    public async Task<IActionResult> DeleteTable(Guid restaurantId, Guid tableId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var result = await _mainRestaurantServices.DeleteTable(restaurantId, tableId, ownerId);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(result);
    }

    //Allows the admin to delete a restaurant
    [HttpDelete("{restaurantId:guid}/delete-restaurant")]
    public async Task<IActionResult> DeleteRestaurant(Guid restaurantId)
    {
        var isAdmin = User.FindFirst("isAdmin")?.Value;
        var adminClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (isAdmin != "true" && adminClaim != "Admin") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var result = await _mainRestaurantServices.DeleteRestaurant(restaurantId);
        if(result.Success == false){
            return NotFound(new { message = result.Message });
        }

        return Ok(result);
    }
}
