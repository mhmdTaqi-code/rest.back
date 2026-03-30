namespace SmartDiningSystem.Application.DTOs.RestaurantTables;

public class RestaurantTableDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalRatingsCount { get; set; }
    public int TableNumber { get; set; }
    public string? ImageUrl { get; set; }
    public string TableToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
