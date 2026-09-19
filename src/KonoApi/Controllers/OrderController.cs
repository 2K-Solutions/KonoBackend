using Kono.Identity.Domain.Restaurants;
using Kono.Orders.Domain;
using Kono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly KonoDbContext _context;

    public OrderController(KonoDbContext context)
    {
        _context = context;
    }

    [HttpGet("food/{restaurantId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetFoodItems(Guid restaurantId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user is null) return Unauthorized();
        if (user.RestaurantId != restaurantId) return Forbid();

        var foodItems = await _context.MenuItems
            .Where(m => m.RestaurantId == restaurantId && !m.IsDrink)
            .ToListAsync();

        return Ok(foodItems);
    }

    [HttpGet("drinks/{restaurantId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetDrinkItems(Guid restaurantId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user is null) return Unauthorized();
        if (user.RestaurantId != restaurantId) return Forbid();

        var drinkItems = await _context.MenuItems
            .Where(m => m.RestaurantId == restaurantId && m.IsDrink)
            .ToListAsync();

        return Ok(drinkItems);
    }
}