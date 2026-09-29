using Kono.Infrastructure.Persistence;
using Kono.Orders.Domain;
using Kono.Orders.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Orders.Repositories;

public class OrderInfoRepository : IOrderInfoRepository
{
    private readonly KonoDbContext _context;

    public OrderInfoRepository(KonoDbContext context) => _context = context;

    public async Task<List<OrderInfo>> GetAllOrdersAsync()
    {
        return await _context.OrderInfo.ToListAsync();
    }

    public async Task<OrderInfo> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _context.OrderInfo.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }
        return order;
    }

    public async Task AddOrderAsync(OrderInfo order)
    {
        await _context.OrderInfo.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOrderAsync(OrderInfo order)
    {
        _context.OrderInfo.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOrderAsync(Guid orderId)
    {
        var order = await GetOrderByIdAsync(orderId);
        _context.OrderInfo.Remove(order);
        await _context.SaveChangesAsync();
    }
}