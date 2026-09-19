namespace Kono.Restaurants.Domain;

public class OrderInfo
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemDescription { get; set; }
    public int Quantity { get; set; }
}