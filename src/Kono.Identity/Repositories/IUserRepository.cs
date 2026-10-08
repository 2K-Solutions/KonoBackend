using Kono.Identity.Domain.Users;

namespace Kono.Identity.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
    Task SaveChangesAsync();
    Task<List<User>> GetUnemployedUsersAsync();
}
