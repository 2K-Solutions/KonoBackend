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

    //Allows the owner to get all food items for a specific restaurant
    [HttpGet("{restaurantId}/get-food-items")]
    public async Task<IActionResult> GetAllFoodItems(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner" || accountType != "worker") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var userId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if(restaurant == null) return NotFound();
        if(accountType == "owner" && restaurant.OwnerId != userId) return Forbid();
        if(accountType == "worker" && _restaurantRepository.GetRestaurantUserByIdAsync(userId) == null) return Forbid();

        var foodItems = await _menuRepository.GetFoodItemsByRestaurantIdAsync(restaurantId);
        return Ok(foodItems);
    }

    //Allows the owner to get all drink items for a specific restaurant
    [HttpGet("{restaurantId}/get-drink-items")]
    public async Task<IActionResult> GetAllDrinkItems(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner" || accountType != "worker") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var userId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if(restaurant == null) return NotFound();
        if(accountType == "owner" && restaurant.OwnerId != userId) return Forbid();
        if(accountType == "worker" && _restaurantRepository.GetRestaurantUserByIdAsync(userId) == null) return Forbid();

        var drinkItems = await _menuRepository.GetDrinkItemsByRestaurantIdAsync(restaurantId);
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

        _context.MenuItem.Remove(menuItem);
        await _context.SaveChangesAsync();

        return NoContent();
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