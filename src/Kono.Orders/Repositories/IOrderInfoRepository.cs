using Kono.Orders.Domain;

namespace Kono.Orders.Repositories;

public interface IOrderInfoRepository
{
    Task<List<OrderInfo>> GetAllOrdersAsync();
    Task<OrderInfo> GetOrderByIdAsync(Guid orderId);
    Task AddOrderAsync(OrderInfo order);
    Task UpdateOrderAsync(OrderInfo order);
    Task DeleteOrderAsync(Guid orderId);
}