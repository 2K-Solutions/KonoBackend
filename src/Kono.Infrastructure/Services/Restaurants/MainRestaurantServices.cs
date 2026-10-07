using Kono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kono.Restaurants.Domain;
using Kono.Restaurants.Repositories;
using Kono.Identity.Repositories;
using KonoInfrastructure.Contracts.Restaurants;

namespace Kono.Infrastructure.Restaurants.Services;

public class MainRestaurantServices
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IOwnerRepository _ownerRepository;

    public MainRestaurantServices(KonoDbContext context, 
        IRestaurantRepository restaurantRepository, 
        IOwnerRepository ownerRepository)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _ownerRepository = ownerRepository;
    }

    

    public async Task<List<RestaurantBasic>> GetRestaurant(Guid ownerId)
    {
        var restaurants = await _restaurantRepository.GetRestaurantsByOwnerIdAsync(ownerId);

        return restaurants
            .Select(r => new RestaurantBasic(r.Id, r.RestaurantName, r.City, r.Address))
            .ToList();
    }
}