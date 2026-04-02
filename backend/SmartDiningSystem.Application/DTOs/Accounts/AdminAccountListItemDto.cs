namespace SmartDiningSystem.Application.DTOs.Accounts;

public class AdminAccountListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string? RestaurantApprovalStatus { get; set; }
    public int OwnedRestaurantCount { get; set; }
    public IReadOnlyList<AdminOwnedRestaurantSummaryDto> OwnedRestaurants { get; set; }
        = Array.Empty<AdminOwnedRestaurantSummaryDto>();
    public DateTime CreatedAtUtc { get; set; }
}
