using Kono.Identity.Domain.Owners;

namespace Kono.Identity.Repositories;

public interface IOwnerRepository
{
    Task<Owner?> GetByEmailAsync(string email);
    Task<Owner?> GetByIdAsync(Guid id);
    Task AddAsync(Owner owner);
    Task<bool> ExistsByEmailAsync(string email);
    Task SaveChangesAsync();
}
