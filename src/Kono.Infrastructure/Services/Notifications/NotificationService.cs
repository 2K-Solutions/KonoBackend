using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Kono.Identity.Domain.DeviceTokens;
using Kono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kono.Infrastructure.Services.Notifications;

public interface INotificationService
{
    Task RegisterDeviceAsync(Guid userId, string token, string? platform);
    Task RemoveDeviceAsync(Guid userId, string token);
    Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null);
}

public class NotificationService : INotificationService
{
    private readonly KonoDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(KonoDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RegisterDeviceAsync(Guid userId, string token, string? platform)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var existing = await _context.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token);
        if (existing != null)
        {
            existing.UserId = userId;
            existing.Platform = platform;
            existing.UpdatedAt = now;
        }
        else
        {
            _context.DeviceTokens.Add(new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                Platform = platform,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveDeviceAsync(Guid userId, string token)
    {
        await _context.DeviceTokens
            .Where(d => d.UserId == userId && d.Token == token)
            .ExecuteDeleteAsync();
    }

	public async Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("Firebase is not configured; skipping notification to user {UserId}", userId);
            return;
        }

        try
        {
            var tokens = await _context.DeviceTokens
                .Where(d => d.UserId == userId)
                .Select(d => d.Token)
                .ToListAsync();

            if (tokens.Count == 0) return;

            #pragma warning disable CS0618
            var message = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new Notification { Title = title, Body = body },
                Data = data
            };

            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

            // Drop tokens FCM says are dead (app uninstalled, token rotated), so we stop sending to them.
            var deadTokens = response.Responses
                .Select((r, i) => (r, Token: tokens[i]))
                .Where(x => !x.r.IsSuccess &&
                            x.r.Exception?.MessagingErrorCode is MessagingErrorCode.Unregistered
                                                              or MessagingErrorCode.SenderIdMismatch)
                .Select(x => x.Token)
                .ToList();

            if (deadTokens.Count > 0)
            {
                await _context.DeviceTokens
                    .Where(d => deadTokens.Contains(d.Token))
                    .ExecuteDeleteAsync();
            }

            if (response.FailureCount > 0)
            {
                _logger.LogWarning("Notification to user {UserId}: {Failed} of {Total} deliveries failed",
                    userId, response.FailureCount, tokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }
}
