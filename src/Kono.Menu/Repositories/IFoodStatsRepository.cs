using Kono.Menu.Domain;

namespace Kono.Menu.Repositories;

public interface IFoodStatsRepository
{
    Task AddFoodStatisticsAsync(FoodStatistics food);
    Task AddDrinkStatisticsAsync(DrinkStatistics drink);
    Task<List<FoodStatistics>> GetFoodStatisticsAsync(Guid menuItemId, DateTime startDate, DateTime endDate);
    Task<List<DrinkStatistics>> GetDrinkStatisticsAsync(Guid menuItemId, DateTime startDate, DateTime endDate);
    
}