namespace SmartDiningSystem.Application.DTOs.Accounts;

public class AdminOwnedRestaurantSummaryDto
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
}
