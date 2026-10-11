using Kono.Infrastructure.Persistence;
using Kono.Identity.Domain.Owners;
using Kono.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Auth.Repositories;

public class OwnerRepository : IOwnerRepository
{
    private readonly KonoDbContext _context;

    public OwnerRepository(KonoDbContext context) => _context = context;

    public Task<Owner?> GetByEmailAsync(string email)
        => _context.Owners.FirstOrDefaultAsync(o => o.Email == email && o.DeletedAt == null);

    public Task<Owner?> GetByIdAsync(Guid id)
        => _context.Owners.FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

    public Task AddAsync(Owner owner)
    {
        _context.Owners.Add(owner);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string email)
        => _context.Owners.AnyAsync(o => o.Email == email && o.DeletedAt == null);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
