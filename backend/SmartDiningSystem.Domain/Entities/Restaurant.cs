using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public RestaurantApprovalStatus ApprovalStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }

    public UserAccount? Owner { get; set; }
    public ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
    public ICollection<TableCart> TableCarts { get; set; } = new List<TableCart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<RestaurantRating> Ratings { get; set; } = new List<RestaurantRating>();
}
