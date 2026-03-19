namespace SmartDiningSystem.Application.Areas.Admin.Models;

public class AdminAccountDetailsViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantApprovalStatus { get; set; }
    public string? RestaurantRejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
