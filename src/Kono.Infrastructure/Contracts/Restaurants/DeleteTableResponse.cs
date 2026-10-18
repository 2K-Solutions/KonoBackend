namespace Kono.Infrastructure.Contracts.Restaurants;

public sealed record DeleteTableResponse
{
    public bool Success;
    public string Message = string.Empty;
}