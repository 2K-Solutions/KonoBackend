using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kono.Infrastructure.Contracts.Orders;
using Kono.Orders.Domain;
using Kono.Infrastructure.Persistence;
using Kono.Orders.Repositories;

namespace Kono.Infrastructure.Services.Orders;

public class MainOrderService
{
    private readonly KonoDbContext _context;
    private readonly IOrdersRepository _orderRepository;

    public MainOrderService(KonoDbContext context, IOrdersRepository orderRepository)
    {
        _context = context;
        _orderRepository = orderRepository;
    }

    public async Task<bool> CheckUserCredibility(Guid restaurantId, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if(user.RestaurantId != restaurantId)
            return false;
        return true;
    }

    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, Guid userId)
    {
        if (request.Items is null || request.Items.Count == 0)
            return CreateOrderResult.Fail(ResponseError.BadRequest, "Order must contain at least one item");

        if (request.Items.Any(i => i.Quantity <= 0))
            return CreateOrderResult.Fail(ResponseError.BadRequest, "Quantity must be greater than 0");

        var drinkFlags = await GetDrinkFlagsAsync(request.RestaurantId, request.Items.Select(i => i.MenuItemId));
        if (drinkFlags is null)
            return CreateOrderResult.Fail(ResponseError.BadRequest, "One or more menu items do not exist in this restaurant");

        var orderId = Guid.NewGuid();

        var order = new CurrentOrders
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            RestaurantId = request.RestaurantId,
            TableId = request.TableId,
            UserId = userId,
            IsActive = true
        };

        var items = request.Items.Select(i => new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            MenuItemId = i.MenuItemId,
            IsDrink = drinkFlags[i.MenuItemId],
            Quantity = i.Quantity,
            ItemDescription = i.ItemDescription ?? string.Empty
        }).ToList();

        await _context.CurrentOrders.AddAsync(order);
        await _context.OrderItems.AddRangeAsync(items);
        await _context.SaveChangesAsync();   

        return CreateOrderResult.Ok(new CreateOrderResponse(orderId));
    }

    private async Task<Dictionary<Guid, bool>?> GetDrinkFlagsAsync(Guid restaurantId, IEnumerable<Guid> menuItemIds)
    {
        var ids = menuItemIds.Distinct().ToList();

        var flags = await _context.MenuItem
            .Where(m => m.RestaurantId == restaurantId && ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.IsDrink);

        return flags.Count == ids.Count ? flags : null;
    }
}   