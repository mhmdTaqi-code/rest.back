namespace SmartDiningSystem.Domain.Entities;

public class MenuCategory
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Restaurant? Restaurant { get; set; }
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
