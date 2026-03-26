namespace SmartDiningSystem.Application.Areas.Admin.Models;

public class AdminAccountListItemViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string? RestaurantApprovalStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
