namespace Kono.Restaurants.Domain;

public class Tables
{
    public Guid Id { get; set; }
    public short TableNumber { get; set; }
    public Guid RestaurantId { get; set; }
    public TableState TableState { get; set; }
}

public enum TableState : short
{
    Available = 0,
    Occupied = 1,
    Reserved = 2
}