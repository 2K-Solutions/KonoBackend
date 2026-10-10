using Kono.Orders.Domain;

namespace Kono.Orders.Repositories;
    public interface IOrdersRepository
    {
        Task<List<CurrentOrders>> GetCurrentOrdersAsync();
        Task<CurrentOrders> GetOrderByIdAsync(Guid orderId);
        Task AddOrderAsync(CurrentOrders order);
        Task AddItemToOrderAsync(OrderItem item);
        Task UpdateOrderAsync(CurrentOrders order);
        Task DeleteOrderAsync(Guid orderId);
    }
