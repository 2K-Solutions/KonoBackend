namespace Kono.Infrastructure.Contracts.Notifications;

public sealed record RegisterDeviceRequest(string Token, string? Platform);

public sealed record RemoveDeviceRequest(string Token);
