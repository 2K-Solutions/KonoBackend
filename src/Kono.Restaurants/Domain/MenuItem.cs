namespace Kono.Restaurants.Domain;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public bool IsDrink { get; set; }
    public required string Name { get; set; }
}