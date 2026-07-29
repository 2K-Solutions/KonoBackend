using Kono.Identity.Domain.Users;

namespace Kono.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
    Task SaveChangesAsync();
}
