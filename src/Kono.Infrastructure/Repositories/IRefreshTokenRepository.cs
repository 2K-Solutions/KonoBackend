using Kono.Identity.Domain.RefreshTokens;

namespace Kono.Infrastructure.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task UpdateAsync(RefreshToken token);
    Task SaveChangesAsync();
}
