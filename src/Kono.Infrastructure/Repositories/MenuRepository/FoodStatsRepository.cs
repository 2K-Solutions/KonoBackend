using Kono.Menu.Domain;
using Microsoft.EntityFrameworkCore;
using Kono.Infrastructure.Persistence;

namespace Kono.Menu.Repositories;

public class FoodStatsRepository : IFoodStatsRepository
{
    private readonly KonoDbContext _context;

    public FoodStatsRepository(KonoDbContext context)
    {
        _context = context;
    }

    public async Task AddFoodStatisticsAsync(FoodStatistics food)
    {
        _context.FoodStatistics.Add(food);
        await _context.SaveChangesAsync();
    }

    public async Task AddDrinkStatisticsAsync(DrinkStatistics drink)
    {
        _context.DrinkStatistics.Add(drink);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FoodStatistics>> GetFoodStatisticsAsync(Guid menuItemId, DateTime startDate, DateTime endDate)
    {
        return await _context.FoodStatistics
            .Where(fs => fs.MenuItemId == menuItemId && fs.StatDate >= startDate && fs.StatDate <= endDate)
            .OrderBy(fs => fs.StatDate)
            .ToListAsync();
    }

    public async Task<List<DrinkStatistics>> GetDrinkStatisticsAsync(Guid menuItemId, DateTime startDate, DateTime endDate)
    {
        return await _context.DrinkStatistics
            .Where(ds => ds.MenuItemId == menuItemId && ds.StatDate >= startDate && ds.StatDate <= endDate)
            .OrderBy(ds => ds.StatDate)
            .ToListAsync();
    }

    public async Task<List<FoodStatistics>> GetAllFoodStatisticsAsync(Guid restaurantId)
    {
        return await _context.FoodStatistics
            .Join(_context.MenuItem, fs => fs.MenuItemId, mi => mi.Id, (fs, mi) => new { Stat = fs, mi.RestaurantId })
            .Where(x => x.RestaurantId == restaurantId)
            .Select(x => x.Stat)
            .OrderBy(fs => fs.StatDate)
            .ToListAsync();
    }
    public async Task<List<DrinkStatistics>> GetAllDrinkStatisticsAsync(Guid restaurantId)
    {
        return await _context.DrinkStatistics
            .Join(_context.MenuItem, fs => fs.MenuItemId, mi => mi.Id, (fs, mi) => new { Stat = fs, mi.RestaurantId })
            .Where(x => x.RestaurantId == restaurantId)
            .Select(x => x.Stat)
            .OrderBy(fs => fs.StatDate)
            .ToListAsync();
    }
}