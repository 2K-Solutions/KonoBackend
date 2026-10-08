namespace Kono.Infrastructure.Contracts.MenuItems;

public sealed record CreateMenuItemRequest(Guid RestaurantId, string Name, decimal Price, bool IsDrink);
public sealed record MenuItemCreatedResponse(Guid Id, Guid RestaurantId, string Name, decimal Price, bool IsDrink, string SuccessMessage);


