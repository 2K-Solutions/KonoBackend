using Kono.Orders.Domain;
using Kono.Restaurants.Domain;
using Kono.Infrastructure.Persistence;
using Kono.Infrastructure.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kono.Infrastructure.Services.Orders;
using Microsoft.VisualBasic;
using Kono.Orders.Repositories;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly KonoDbContext _context;
    private readonly MainOrderService _mainOrderService;
    private readonly IOrdersRepository _ordersRepository;

    public OrderController(KonoDbContext context, MainOrderService mainOrderService, IOrdersRepository ordersRepository)
    {
        _context = context;
        _mainOrderService = mainOrderService;
        _ordersRepository = ordersRepository;
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

    [HttpGet("{userId}/get-my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var orders = await _ordersRepository.GetOrdersByUserIdAsync(userId);

        return Ok(orders);
    }

    [HttpGet("{orderId}/get-order-items")]
    public async Task<IActionResult> GetOrderItems(Guid orderId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var order = await _ordersRepository.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId) return NotFound();

        var orderItems = await _ordersRepository.GetOrderItemsByOrderIdAsync(orderId);

        return Ok(orderItems);
    }

    [HttpPatch("{orderId}/edit-order-items")]
    public async Task<IActionResult> EditOrderItem(Guid orderId, [FromBody] CreateOrderRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var order = await _ordersRepository.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId) return NotFound();

        var result = await _mainOrderService.EditOrderAsync(orderId, request, userId);

        if (result.error != ResponseError.None)
            return BadRequest(result.message);

        return Ok(result.message);
    }

    [HttpDelete("{orderId}/delete-order")]
    public async Task<IActionResult> DeleteOrderAndItems(Guid orderId)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var order = await _ordersRepository.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId) return NotFound();

        await _ordersRepository.DeleteOrderAsync(order.Id);

        return Ok("Order deleted successfully");
    }
}