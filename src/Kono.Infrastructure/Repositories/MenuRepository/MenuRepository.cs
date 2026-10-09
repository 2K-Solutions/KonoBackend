using Microsoft.EntityFrameworkCore;
using Kono.Menu.Domain;
using Kono.Menu.Repositories;
using Kono.Infrastructure.Persistence;

namespace Kono.Menu.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly KonoDbContext _context;

    public MenuRepository(KonoDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItem>> GetAllMenuItemsAsync()
    {
        return await _context.MenuItem.ToListAsync();
    }

    public async Task<MenuItem?> GetMenuItemByIdAsync(Guid menuItemId)
    {
        return await _context.MenuItem.FindAsync(menuItemId);
    }

    public async Task AddMenuItemAsync(MenuItem menuItem)
    {
        _context.MenuItem.Add(menuItem);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMenuItemAsync(MenuItem menuItem)
    {
        _context.MenuItem.Update(menuItem);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteMenuItemAsync(Guid menuItemId)
    {
        var menuItem = await _context.MenuItem.FindAsync(menuItemId);
        if (menuItem != null)
        {
            _context.MenuItem.Remove(menuItem);
            await _context.SaveChangesAsync();
        }
    }
}