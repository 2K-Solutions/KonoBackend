namespace Kono.Identity.Domain.Restaurants;

public class Restaurant
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
