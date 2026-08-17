namespace Kono.Identity.Domain.Restaurants;

public enum RestaurantInviteStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Expired = 4
}

public class RestaurantInvite
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid UserId { get; set; }
    public RestaurantInviteStatus Status { get; set; } = RestaurantInviteStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
