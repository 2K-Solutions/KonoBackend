using Kono.Orders.Domain;

namespace Kono.Infrastructure.Contracts.Orders;

public sealed record OrderItemRequest(Guid MenuItemId,
                                      int Quantity,
                                      string ItemDescription);

public sealed record CreateOrderRequest(Guid RestaurantId,
                                        Guid TableId,
                                        List<OrderItemRequest> Items);