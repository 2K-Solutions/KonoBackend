namespace Kono.Identity.Domain.Restaurants;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public bool IsDrink { get; set; }
    public string Name { get; set; }
}