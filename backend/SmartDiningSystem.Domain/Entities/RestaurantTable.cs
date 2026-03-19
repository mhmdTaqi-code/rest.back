namespace SmartDiningSystem.Domain.Entities;

public class RestaurantTable
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public Restaurant? Restaurant { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
