namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class AdminRestaurantDetailsDto
{
    public Guid Id { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string? RestaurantDescription { get; set; }
    public string RestaurantAddress { get; set; } = string.Empty;
    public string RestaurantPhoneNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhoneNumber { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
}
