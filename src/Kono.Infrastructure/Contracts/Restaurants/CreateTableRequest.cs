namespace Kono.Infrastructure.Contracts.Restaurants;

public sealed record AddTableRequest(Guid RestaurantId, short TableNumber);

public sealed record EditTableRequest(Guid RestaurantId, Guid TableId, short TableNumber);