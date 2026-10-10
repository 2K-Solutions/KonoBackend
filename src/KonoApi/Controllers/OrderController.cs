using Kono.Orders.Domain;
using Kono.Restaurants.Domain;
using Kono.Infrastructure.Persistence;
using KonoInfrastructure.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly KonoDbContext _context;

    public OrderController(KonoDbContext context)
    {
        _context = context;
    }

    
}