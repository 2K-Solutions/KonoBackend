namespace Kono.Restaurants.Domain;

public class InviteResult
{
    public bool RestaurantSuccess { get; set; } = true;
    public bool UserSuccess { get; set; } = true;
    public bool PendingInviteSuccess { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public RestaurantInvite invite = new RestaurantInvite{};
}