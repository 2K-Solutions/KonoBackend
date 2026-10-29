namespace Kono.Infrastructure.Contracts.Restaurants;

public record RestaurantAbandonResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}