using Kono.Orders.Domain;
using Kono.Restaurants.Domain;
using Kono.Infrastructure.Persistence;
using Kono.Infrastructure.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kono.Infrastructure.Services.Orders;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly KonoDbContext _context;
    private readonly MainOrderService _mainOrderService;

    public OrderController(KonoDbContext context, MainOrderService mainOrderService)
    {
        _context = context;
        _mainOrderService = mainOrderService;
    }

    [HttpPost("{userId}/create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        if(await _mainOrderService.CheckUserCredibility(request.RestaurantId, userId) == false)
            return Forbid();

        var result = await _mainOrderService.CreateOrderAsync(request, userId);

        if (result.error != ResponseError.None)
            return BadRequest(result.message);

        return Ok(result.message);
    }
}