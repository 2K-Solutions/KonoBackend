using Kono.Restaurants.Domain;
using Kono.Identity.Domain.Users;

namespace Kono.Restaurants.Repositories;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllRestaurantsAsync();
    Task<Restaurant?> GetRestaurantByIdAsync(Guid restaurantId);
    Task AddRestaurantAsync(Restaurant restaurant);
    Task UpdateRestaurantAsync(Restaurant restaurant);
    Task DeleteRestaurantAsync(Guid restaurantId);
    Task<List<Restaurant>> GetRestaurantsByOwnerIdAsync(Guid ownerId);
    Task<List<User>> GetSomeAvailableUsersAsync();
    Task<Guid> GetOwnerIdByRestaurantIdAsync(Guid restaurantId);
    Task<List<User>> GetRestaurantUsersByRestaurantandOwnerIdAsync(Guid restaurantId, Guid ownerId);
}