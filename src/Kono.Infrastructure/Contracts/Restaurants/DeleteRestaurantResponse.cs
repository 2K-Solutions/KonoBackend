namespace Kono.Infrastructure.Contracts.Restaurants;

public sealed record DeleteRestaurantResponse
{
    public bool Success;
    public string Message = string.Empty;
}