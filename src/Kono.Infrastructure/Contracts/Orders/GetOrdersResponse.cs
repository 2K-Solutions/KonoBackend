namespace Kono.Infrastructure.Contracts.Orders;

public sealed record GetOrderResponse(Guid OrderId, Guid TableId);