namespace KonoInfrastructure.Contracts.Restaurants;

public sealed record CreateRestaurantRequest(Guid OwnerId, string RestaurantName, string City, string Address);
