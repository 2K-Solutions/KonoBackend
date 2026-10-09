using Microsoft.EntityFrameworkCore;
using Kono.Restaurants.Domain;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Persistence;
using Kono.Identity.Domain.Users;

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

    public async Task<List<User>> GetSomeAvailableUsersAsync()
    {
        return await _context.Users
            .Where(u => u.RestaurantId == null && u.DeletedAt == null)
            .Take(5)
            .ToListAsync();
    }

    public async Task<Guid> GetOwnerIdByRestaurantIdAsync(Guid restaurantId)
    {
        var restaurant = await _context.Restaurants
            .Where(r => r.Id == restaurantId && r.DeletedAt == null)
            .Select(r => r.OwnerId)
            .FirstOrDefaultAsync();
        if (restaurant == Guid.Empty)
        {
            throw new InvalidOperationException("Restaurant not found");
        }

        return restaurant;
    }
    
    public async Task<List<User>> GetRestaurantUsersByRestaurantandOwnerIdAsync(Guid restaurantId, Guid ownerId)
    {
            return await _context.Users
            .Where(u => u.RestaurantId == restaurantId && u.DeletedAt == null)
            .Join(_context.Restaurants, u => u.RestaurantId, r => r.Id, (u, r) => new { User = u, Restaurant = r })
            .Where(ur => ur.Restaurant.OwnerId == ownerId)
            .Select(ur => ur.User)
            .ToListAsync();
    }
}