namespace Kono.Menu.Domain;

public class DrinkStatistics
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public DateTime StatDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}