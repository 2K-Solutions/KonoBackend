using Microsoft.EntityFrameworkCore;
using Kono.Infrastructure.Persistence;
using Kono.Menu.Domain;
using Kono.Infrastructure.Contracts.MenuItems;
using Kono.Menu.Repositories;
using Kono.Restaurants.Domain;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Services.Notifications;

namespace Kono.Infrastructure.Services.MenuItems;

public class MenuItemService
{
    private readonly KonoDbContext _context;
    private readonly IFoodStatsRepository _foodStatsRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly INotificationService _notificationService;

    public MenuItemService(KonoDbContext context,
        IFoodStatsRepository foodStatsRepository,
        IMenuRepository menuRepository,
        IRestaurantRepository restaurantRepository,
        INotificationService notificationService)
    {
        _context = context;
        _foodStatsRepository = foodStatsRepository;
        _menuRepository = menuRepository;
        _restaurantRepository = restaurantRepository;
        _notificationService = notificationService;
    }

    public async Task AddMenuItemAsync(Guid restaurantId, CreateMenuItemRequest menuItem)
    {
        
        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if(restaurant == null)
        {
            throw new ArgumentException($"Restaurant with ID {restaurantId} does not exist.");
        }
        var existingMenuItem = await _context.MenuItem.FirstOrDefaultAsync(mi => restaurantId == mi.RestaurantId && mi.Name == menuItem.Name);
        if(existingMenuItem != null)
        {
            throw new ArgumentException($"Menu item with name {menuItem.Name} already exists for this restaurant.");
        }
        var newitem = new MenuItem
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            Name = menuItem.Name,
            Price = menuItem.Price,
            IsDrink = menuItem.IsDrink
        };

        await _menuRepository.AddMenuItemAsync(newitem);
        await UpdateMenuHashAsync(restaurantId);
    }

    // Removes a menu item and refreshes the restaurant's menu hash
    public async Task RemoveMenuItemAsync(MenuItem menuItem)
    {
        await _menuRepository.DeleteMenuItemAsync(menuItem.Id);
        await UpdateMenuHashAsync(menuItem.RestaurantId);
    }

    // Recomputes the menu hash, stores it on the restaurant and pushes it to the restaurant's workers, Returns: the new hash
    public async Task<string> UpdateMenuHashAsync(Guid restaurantId)
    {
        var restaurant = await _context.Restaurants.FindAsync(restaurantId)
            ?? throw new ArgumentException($"Restaurant with ID {restaurantId} does not exist.");

        var hash = await ComputeMenuHashAsync(restaurantId);
        if (restaurant.MenuHash == hash) return hash;

        restaurant.MenuHash = hash;
        await _context.SaveChangesAsync();

        var workers = await _restaurantRepository.GetRestaurantUsersByRestaurantIdAsync(restaurantId);
        await _notificationService.SendDataToUsersAsync(
            workers.Select(u => u.Id),
            new Dictionary<string, string>
            {
                ["type"] = "menu_updated",
                ["restaurantId"] = restaurantId.ToString(),
                ["menuHash"] = hash
            });

        return hash;
    }

    // Returns the stored menu hash, generating it if the restaurant doesn't have one yet (e.g. seeded data)
    public async Task<string> GetMenuHashAsync(Guid restaurantId)
    {
        var restaurant = await _context.Restaurants.FindAsync(restaurantId)
            ?? throw new ArgumentException($"Restaurant with ID {restaurantId} does not exist.");

        return restaurant.MenuHash ?? await UpdateMenuHashAsync(restaurantId);
    }

    // Compares the device's hash with the current one, Returns: just IsUpToDate, or the new hash and full menu if outdated
    public async Task<MenuHashCheckResponse> CheckMenuHashAsync(Guid restaurantId, string? deviceHash)
    {
        var currentHash = await GetMenuHashAsync(restaurantId);
        if (string.Equals(deviceHash?.Trim(), currentHash, StringComparison.OrdinalIgnoreCase))
        {
            return new MenuHashCheckResponse(true);
        }

        var items = await _context.MenuItem
            .Where(mi => mi.RestaurantId == restaurantId)
            .ToListAsync();

        var dtos = items.Select(mi => new MenuItemDto(mi.Id, mi.Name, mi.Price, mi.IsDrink)).ToList();

        // Hash is computed from the exact items returned, so the device never stores a hash that doesn't match its menu
        return new MenuHashCheckResponse(
            false,
            MenuHasher.Compute(items),
            dtos.Where(d => !d.IsDrink).ToList(),
            dtos.Where(d => d.IsDrink).ToList());
    }

    private async Task<string> ComputeMenuHashAsync(Guid restaurantId)
    {
        var items = await _context.MenuItem
            .Where(mi => mi.RestaurantId == restaurantId)
            .ToListAsync();

        return MenuHasher.Compute(items);
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

    public async Task<FoodQuantityResponse> GetDrinkQuantitySoldAsync(Guid menuItemId, DateTime startDate, DateTime endDate)
    {
        var foodStatistics = await _foodStatsRepository.GetDrinkStatisticsAsync(menuItemId, startDate, endDate);
        if(foodStatistics == null || !foodStatistics.Any())
        {
            return new FoodQuantityResponse(menuItemId, 0, 0);
        }

        int totalQuantitySold = foodStatistics.Sum(fs => fs.TotalOrders);
        decimal totalRevenue = foodStatistics.Sum(fs => fs.TotalRevenue);

        return new FoodQuantityResponse(menuItemId, totalQuantitySold, totalRevenue);
    }
}