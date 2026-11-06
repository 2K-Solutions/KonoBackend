using Kono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Contracts.MenuItems;
using Kono.Menu.Repositories;
using Kono.Infrastructure.Services.MenuItems;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class MenuController : ControllerBase
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IFoodStatsRepository _foodStatsRepository;
    private readonly MenuItemService _menuItemService;
    private readonly IMenuRepository _menuRepository;

    public MenuController(KonoDbContext context, 
                          IFoodStatsRepository foodStatsRepository,
                          IRestaurantRepository restaurantRepository, 
                          MenuItemService menuItemService, 
                          IMenuRepository menuRepository)
    {
        _context = context;
        _foodStatsRepository = foodStatsRepository;
        _restaurantRepository = restaurantRepository;
        _menuItemService = menuItemService;
        _menuRepository = menuRepository;
    }
    //Allows the owner to add a new menu item food or drink
    [HttpPost("{restaurantId}/menu-items/add")]
    public async Task<IActionResult> AddMenuItem(Guid restaurantId, CreateMenuItemRequest menuItem)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId) return Forbid();

        try
        {
            await _menuItemService.AddMenuItemAsync(restaurantId, menuItem);
            return Ok(menuItem);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    //Allows the worker (restaurantId from token) or the owner (restaurantId from query) to get all food items
    [HttpGet("get-food-items")]
    public async Task<IActionResult> GetAllFoodItems()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker" && accountType != "owner") return Forbid();

        var restaurantId = User.FindFirst("restaurantId")?.Value;
        if (!Guid.TryParse(restaurantId, out var restaurantGuid)) return Unauthorized();

        var foodItems = await _menuRepository.GetFoodItemsByRestaurantIdAsync(restaurantGuid);
        return Ok(foodItems);
    }

    //Allows the worker (restaurantId from token) or the owner (restaurantId from query) to get all drink items
    [HttpGet("get-drink-items")]
    public async Task<IActionResult> GetAllDrinkItems()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker" && accountType != "owner") return Forbid();

        var restaurantId = User.FindFirst("restaurantId")?.Value;
        if (!Guid.TryParse(restaurantId, out var restaurantGuid)) return Unauthorized();

        var drinkItems = await _menuRepository.GetDrinkItemsByRestaurantIdAsync(restaurantGuid);
        return Ok(drinkItems);
    }
    //Get quantity sold for a specific menu item within a date range
    [HttpGet("{menuItemId}/item-quantity-sold")]
    public async Task<IActionResult> GetItemQuantitySold(Guid menuItemId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var menuItem = await _context.MenuItem.FindAsync(menuItemId);
        if (menuItem == null) return NotFound();
        try
        {
            if(menuItem.IsDrink)
            {
                var drinkQuantity = await _menuItemService.GetDrinkQuantitySoldAsync(menuItemId, startDate, endDate);
                return Ok(drinkQuantity);
            }
            else
            {
                var foodQuantity = await _menuItemService.GetFoodQuantitySoldAsync(menuItemId, startDate, endDate);
                return Ok(foodQuantity);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    //Allows the owner to remove a menu item from the restaurant's menu
    [HttpDelete("{restaurantId:guid}/menu-items/{menuItemId:guid}/remove")]
    public async Task<IActionResult> RemoveMenuItem(Guid menuItemId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var menuItem = await _context.MenuItem.FindAsync(menuItemId);
        if (menuItem == null) return NotFound();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(menuItem.RestaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId) return Forbid();

        await _menuItemService.RemoveMenuItemAsync(menuItem);

        return NoContent();
    }

    //Mobile app sends the menu hash it has stored; if it doesn't match the restaurant's current hash the app should refetch the menu.
    [HttpGet("menu-hash/check")]
    public async Task<IActionResult> CheckMenuHash([FromQuery] string? hash)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var restaurantId = User.FindFirst("restaurantId")?.Value;
        if (!Guid.TryParse(restaurantId, out var restaurantGuid)) return Unauthorized();

        try
        {
            var result = await _menuItemService.CheckMenuHashAsync(restaurantGuid, hash);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    //Allows the owner to get all food statistics for a specific restaurant within a date range
    [HttpGet("{restaurantId}/get-all-food-statistics")]
    public async Task<IActionResult> GetFoodStatistics(Guid restaurantId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId) return Forbid();

        var foodStatistics = await _foodStatsRepository.GetAllFoodStatisticsAsync(restaurantId, startDate, endDate);
        return Ok(foodStatistics);
    }

    //Allows the owner to get all drink statistics for a specific restaurant within a date range
    [HttpGet("{restaurantId}/get-all-drink-statistics")]
    public async Task<IActionResult> GetDrinkStatistics(Guid restaurantId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId) return Forbid();

        var drinkStatistics = await _foodStatsRepository.GetAllDrinkStatisticsAsync(restaurantId, startDate, endDate);
        return Ok(drinkStatistics);
    }
}
