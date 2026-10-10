namespace Kono.Infrastructure.Contracts.Orders;

public sealed record MenuItemResponse(Guid Id, Guid RestaurantId, string Name);
