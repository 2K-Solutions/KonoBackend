using Microsoft.EntityFrameworkCore;
using Kono.Infrastructure.Persistence;
using Kono.Menu.Domain;
using Kono.Infrastructure.Contracts.MenuItems;
using Kono.Menu.Repositories;
using Kono.Restaurants.Domain;

namespace Kono.Infrastructure.Services.MenuItems;

public class MenuItemService
{
    private readonly KonoDbContext _context;
    private readonly IFoodStatsRepository _foodStatsRepository;
    private readonly IMenuRepository _menuRepository;

    public MenuItemService(KonoDbContext context, IFoodStatsRepository foodStatsRepository, IMenuRepository menuRepository)
    {
        _context = context;
        _foodStatsRepository = foodStatsRepository;
        _menuRepository = menuRepository;
    }

    public async Task AddMenuItemAsync(MenuItem menuItem)
    {
        var restaurant = await _context.Restaurants.FindAsync(menuItem.RestaurantId);
        if(restaurant == null)
        {
            throw new ArgumentException($"Restaurant with ID {menuItem.RestaurantId} does not exist.");
        }
        var existingMenuItem = await _context.MenuItem.FirstOrDefaultAsync(mi => restaurant.Id == mi.RestaurantId && mi.Name == menuItem.Name);
        if(existingMenuItem != null)
        {
            throw new ArgumentException($"Menu item with name {menuItem.Name} already exists for this restaurant.");
        }
        await _menuRepository.AddMenuItemAsync(menuItem);
    }

    public async Task<FoodQuantityResponse> GetFoodQuantitySoldAsync(Guid menuItemId, DateTime startDate, DateTime endDate)
    {
        var foodStatistics = await _foodStatsRepository.GetFoodStatisticsAsync(menuItemId, startDate, endDate);
        if(foodStatistics == null || !foodStatistics.Any())
        {
            return new FoodQuantityResponse(menuItemId, 0, 0);
        }

        int totalQuantitySold = foodStatistics.Sum(fs => fs.TotalOrders);
        decimal totalRevenue = foodStatistics.Sum(fs => fs.TotalRevenue);

        return new FoodQuantityResponse(menuItemId, totalQuantitySold, totalRevenue);
    }
}