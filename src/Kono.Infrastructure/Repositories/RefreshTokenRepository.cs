using Kono.Infrastructure.Persistence;
using Kono.Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly KonoDbContext _context;

    public RefreshTokenRepository(KonoDbContext context) => _context = context;

    public Task AddAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token)
        => _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

    public Task UpdateAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
