namespace SmartDiningSystem.Domain.Entities;

public class BookingItem
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Booking? Booking { get; set; }
    public MenuItem? MenuItem { get; set; }
}
