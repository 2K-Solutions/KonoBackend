using Microsoft.EntityFrameworkCore;
using Kono.Restaurants.Domain;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Persistence;

namespace Kono.Infrastructure.Repositories.RestaurantRepository;

public class RestaurantRepo : IRestaurantRepository
{
    private readonly KonoDbContext _context;

    public RestaurantRepo(KonoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Restaurant>> GetAllRestaurantsAsync()
    {
        return await _context.Restaurants.ToListAsync();
    }

    public async Task<Restaurant?> GetRestaurantByIdAsync(Guid restaurantId)
    {

        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if(restaurant == null){
            return null;
        }
        return restaurant;
    }

    public async Task<List<Restaurant>> GetRestaurantsByOwnerIdAsync(Guid ownerId)
    {
        return await _context.Restaurants
            .Where(r => r.OwnerId == ownerId && r.DeletedAt == null)
            .ToListAsync();
    }

    public async Task AddRestaurantAsync(Restaurant restaurant)
    {
        await _context.Restaurants.AddAsync(restaurant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRestaurantAsync(Restaurant restaurant)
    {
        _context.Restaurants.Update(restaurant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRestaurantAsync(Guid restaurantId)
    {
        var restaurant = await GetRestaurantByIdAsync(restaurantId);
        if (restaurant != null)
        {
            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();
        }
    }
}