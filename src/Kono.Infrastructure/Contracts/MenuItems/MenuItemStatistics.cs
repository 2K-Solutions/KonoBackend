namespace Kono.Infrastructure.Contracts.MenuItems;

public sealed record FoodQuantityResponse(Guid MenuItemId, int QuantitySold, decimal TotalRevenue);