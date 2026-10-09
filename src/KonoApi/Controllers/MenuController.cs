using Kono.Restaurants.Domain;
using Kono.Infrastructure.Persistence;
using KonoInfrastructure.Contracts.Auth;
using KonoInfrastructure.Contracts.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kono.Infrastructure.Repositories.RestaurantRepository;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Restaurants.Services;
using Kono.Identity.Repositories;
using Kono.Infrastructure.Auth.Services;
using Kono.Infrastructure.Contracts.MenuItems;
using Kono.Menu.Domain;
using Kono.Menu.Repositories;
using Kono.Infrastructure.Services.MenuItems;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class MenuController : ControllerBase
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly MenuItemService _menuItemService;
    private readonly IFoodStatsRepository _foodStatsRepository;

    public MenuController(KonoDbContext context, IRestaurantRepository restaurantRepository, MenuItemService menuItemService, IFoodStatsRepository foodStatsRepository)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _menuItemService = menuItemService;
        _foodStatsRepository = foodStatsRepository;
    }


}