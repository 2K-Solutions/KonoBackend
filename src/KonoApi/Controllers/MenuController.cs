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
    private readonly MenuItemService _menuItemService;
    private readonly IFoodStatsRepository _foodStatsRepository;
    private readonly IMenuRepository _menuRepository;

    public MenuController(KonoDbContext context, 
                          IRestaurantRepository restaurantRepository, 
                          MenuItemService menuItemService, 
                          IFoodStatsRepository foodStatsRepository, 
                          IMenuRepository menuRepository)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _menuItemService = menuItemService;
        _foodStatsRepository = foodStatsRepository;
        _menuRepository = menuRepository;
    }

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
    [HttpGet("{restaurantId}/get-food-items")]
    public async Task<IActionResult> GetAllFoodItems(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId) return Forbid();

        var foodItems = await _menuRepository.GetFoodItemsByRestaurantIdAsync(restaurantId);
        return Ok(foodItems);
    }

    [HttpGet("{restaurantId}/get-drink-items")]
    public async Task<IActionResult> GetAllDrinkItems(Guid restaurantId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "owner") return Forbid();

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(ownerIdClaim, out var ownerId)) return Unauthorized();

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId) return Forbid();

        var drinkItems = await _menuRepository.GetDrinkItemsByRestaurantIdAsync(restaurantId);
        return Ok(drinkItems);
    }

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

    [HttpDelete("{restaurantId}/menu-items/{menuitem:Id}/remove")]
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

}