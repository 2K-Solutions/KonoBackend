using Kono.Infrastructure.Persistence;
using Kono.Identity.Domain.Owners;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Repositories;

public class OwnerRepository : IOwnerRepository
{
    private readonly KonoDbContext _context;

    public OwnerRepository(KonoDbContext context) => _context = context;

    public Task<Owner?> GetByEmailAsync(string email)
        => _context.Owners.FirstOrDefaultAsync(o => o.Email == email && o.DeletedAt == null);

    public Task<Owner?> GetByIdAsync(Guid id)
        => _context.Owners.FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);
}
