namespace SmartDiningSystem.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid? MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsHighlighted { get; set; }
    public string? HighlightTag { get; set; }
    public bool IsAvailable { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Restaurant? Restaurant { get; set; }
    public MenuCategory? MenuCategory { get; set; }
    public ICollection<TableCartItem> TableCartItems { get; set; } = new List<TableCartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
