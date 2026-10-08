namespace KonoInfrastructure.Contracts.Restaurants;

public sealed record CreateRestaurantInviteRequest(Guid RestaurantId, Guid UserId);
