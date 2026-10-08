namespace Kono.Orders.Domain;

public class OrderInfo
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public bool IsDrink { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
}