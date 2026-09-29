using Kono.Restaurants.Domain;

namespace Kono.Restaurants.Repositories;

public interface IMenuItemRepository
{
    Task<List<MenuItem>> GetAllMenuItemsAsync();
    Task<MenuItem> GetMenuItemByIdAsync(Guid menuItemId);
    Task AddMenuItemAsync(MenuItem menuItem);
    Task UpdateMenuItemAsync(MenuItem menuItem);
    Task DeleteMenuItemAsync(Guid menuItemId);
}