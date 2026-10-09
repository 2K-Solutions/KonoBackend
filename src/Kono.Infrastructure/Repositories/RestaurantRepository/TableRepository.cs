using Microsoft.EntityFrameworkCore;
using Kono.Restaurants.Domain;
using Kono.Restaurants.Repositories;
using Kono.Infrastructure.Persistence;

namespace Kono.Infrastructure.Repositories.RestaurantRepository;

public class TableRepository : ITableRepository
{
    private readonly KonoDbContext _context;

    public TableRepository(KonoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tables>> GetTablesByRestaurantIdAsync(Guid restaurantId)
    {
        return await _context.Tables
            .Where(t => t.RestaurantId == restaurantId)
            .ToListAsync();
    }

    public async Task<Tables?> GetTableByIdAsync(Guid tableId)
    {
        return await _context.Tables.FindAsync(tableId);
    }

    public async Task AddTableAsync(Tables table)
    {
        _context.Tables.Add(table);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTableAsync(Tables table)
    {
        _context.Entry(table).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTableAsync(Guid tableId)
    {
        var table = await _context.Tables.FindAsync(tableId);
        if (table != null)
        {
            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
        }
    }
}