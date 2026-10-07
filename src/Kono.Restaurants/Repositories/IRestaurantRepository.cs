using Kono.Restaurants.Domain;

namespace Kono.Restaurants.Repositories;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllRestaurantsAsync();
    Task<Restaurant?> GetRestaurantByIdAsync(Guid restaurantId);
    Task AddRestaurantAsync(Restaurant restaurant);
    Task UpdateRestaurantAsync(Restaurant restaurant);
    Task DeleteRestaurantAsync(Guid restaurantId);
    Task<List<Restaurant>> GetRestaurantsByOwnerIdAsync(Guid ownerId);
}