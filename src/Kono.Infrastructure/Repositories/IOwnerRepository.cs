using Kono.Identity.Domain.Owners;

namespace Kono.Infrastructure.Repositories;

public interface IOwnerRepository
{
    Task<Owner?> GetByEmailAsync(string email);
    Task<Owner?> GetByIdAsync(Guid id);
}
