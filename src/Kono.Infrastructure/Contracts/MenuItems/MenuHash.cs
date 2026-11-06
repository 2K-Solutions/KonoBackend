using System.Text.Json.Serialization;

namespace Kono.Infrastructure.Contracts.MenuItems;

// When up to date only IsUpToDate is serialized; otherwise the new hash and the current menu are included
public sealed record MenuHashCheckResponse(
    bool IsUpToDate,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MenuHash = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<MenuItemDto>? FoodItems = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<MenuItemDto>? DrinkItems = null);

// Full menu for a restaurant along with the hash computed from exactly these items
public sealed record MenuWithHashResponse(
    string MenuHash,
    List<MenuItemDto> FoodItems,
    List<MenuItemDto> DrinkItems);

public sealed record MenuItemDto(Guid Id, string Name, decimal Price, bool IsDrink);
