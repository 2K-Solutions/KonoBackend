using Kono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kono.Infrastructure.Restaurants.Services;

public class RestaurantInviteCleanupService : BackgroundService
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RestaurantInviteCleanupService> _logger;

    public RestaurantInviteCleanupService(IServiceScopeFactory scopeFactory, ILogger<RestaurantInviteCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RunInterval);

        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<KonoDbContext>();
                var cutoff = DateTime.UtcNow - RetentionPeriod;

                await context.RestaurantInvites
                    .Where(i => i.CreatedAt < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invite cleanup failed; retrying in {Interval}", RunInterval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
