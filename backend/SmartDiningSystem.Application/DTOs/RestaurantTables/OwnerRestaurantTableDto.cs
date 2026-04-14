namespace SmartDiningSystem.Application.DTOs.RestaurantTables;

public class OwnerRestaurantTableDto
{
    public Guid TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Zone { get; set; }
    public int? FloorNumber { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
    public bool IsOrderingEnabled { get; set; }
    public bool IsOccupied { get; set; }
    public Guid? ActiveSessionId { get; set; }
    public DateTime? OccupiedSinceUtc { get; set; }
    public Guid? CurrentBookingId { get; set; }
    public int CurrentOrderCount { get; set; }
}
