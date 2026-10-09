using Microsoft.EntityFrameworkCore;
using Kono.Restaurants.Domain;

namespace Kono.Restaurants.Repositories;

public interface ITableRepository
{
    Task<List<Tables>> GetTablesByRestaurantIdAsync(Guid restaurantId);
    Task<Tables?> GetTableByIdAsync(Guid tableId);
    Task AddTableAsync(Tables table);
    Task UpdateTableAsync(Tables table);
    Task DeleteTableAsync(Guid tableId);
}