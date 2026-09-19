namespace Kono.Orders.Domain;

public class CurrentOrders
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public Guid TableId { get; set; }
    public bool IsActive { get; set; }
}