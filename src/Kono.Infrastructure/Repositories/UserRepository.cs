using Kono.Infrastructure.Persistence;
using Kono.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly KonoDbContext _context;

    public UserRepository(KonoDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email)
        => _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null);

    public Task<User?> GetByIdAsync(Guid id)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

    public Task AddAsync(User user)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string email)
        => _context.Users.AnyAsync(u => u.Email == email && u.DeletedAt == null);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
