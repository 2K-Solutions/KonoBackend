using Kono.Orders.Domain;

namespace Kono.Orders.Repositories;
    public interface ICurrentOrdersRepository
    {
        Task<List<CurrentOrders>> GetCurrentOrdersAsync();
        Task<CurrentOrders> GetOrderByIdAsync(Guid orderId);
        Task AddOrderAsync(CurrentOrders order);
        Task UpdateOrderAsync(CurrentOrders order);
        Task DeleteOrderAsync(Guid orderId);
    }
