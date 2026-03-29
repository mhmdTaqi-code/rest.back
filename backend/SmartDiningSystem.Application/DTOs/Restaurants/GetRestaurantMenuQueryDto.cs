namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class GetRestaurantMenuQueryDto
{
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool? IsAvailable { get; set; }
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }
}
