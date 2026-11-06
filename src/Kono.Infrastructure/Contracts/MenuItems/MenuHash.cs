using System.Text.Json.Serialization;

namespace Kono.Infrastructure.Contracts.MenuItems;

// When up to date only IsUpToDate is serialized; otherwise the new hash and the current menu are included
public sealed record MenuHashCheckResponse(
    bool IsUpToDate,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MenuHash = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<MenuItemDto>? FoodItems = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<MenuItemDto>? DrinkItems = null);

public sealed record MenuItemDto(Guid Id, string Name, decimal Price, bool IsDrink);
