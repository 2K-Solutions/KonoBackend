using Kono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kono.Infrastructure.Services;

public class RestaurantInviteCleanupService : BackgroundService
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;

    public RestaurantInviteCleanupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RunInterval);

        do
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<KonoDbContext>();
            var cutoff = DateTime.UtcNow - RetentionPeriod;

            await context.RestaurantInvites
                .Where(i => i.CreatedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
