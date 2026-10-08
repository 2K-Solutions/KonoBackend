using Kono.Menu.Domain;

namespace Kono.Menu.Repositories;

public interface IMenuRepository
{
    Task<List<MenuItem>> GetAllMenuItemsAsync();
    Task<MenuItem?> GetMenuItemByIdAsync(Guid menuItemId);
    Task AddMenuItemAsync(MenuItem menuItem);
    Task UpdateMenuItemAsync(MenuItem menuItem);
    Task DeleteMenuItemAsync(Guid menuItemId);

}