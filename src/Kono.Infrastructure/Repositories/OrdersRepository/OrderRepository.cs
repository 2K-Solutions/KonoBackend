using Kono.Infrastructure.Persistence;
using Kono.Orders.Domain;
using Kono.Orders.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Orders.Repositories;

public class OrderRepository : IOrdersRepository
{
    private readonly KonoDbContext _context;

    public OrderRepository(KonoDbContext context) => _context = context;

    public async Task<List<CurrentOrders>> GetCurrentOrdersAsync()
    {
        return await _context.CurrentOrders.ToListAsync();
    }

    public async Task<CurrentOrders> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _context.CurrentOrders.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }
        return order;
    }

    public async Task AddOrderAsync(CurrentOrders order)
    {
        await _context.CurrentOrders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task AddItemsToOrderAsync(List<OrderItem> items)
    {
        await _context.OrderItems.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOrderAsync(CurrentOrders order)
    {
        _context.CurrentOrders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOrderAsync(Guid currentOrderId)
    {
        var order = await GetOrderByIdAsync(currentOrderId);
        _context.CurrentOrders.Remove(order);
        await _context.SaveChangesAsync();
    }
}