namespace KonoInfrastructure.Contracts.Restaurants;

public sealed record PendingInviteResponse(Guid Id, Guid UserId, Guid RestaurantId, string RestaurantName, DateTime ExpiresAt);
