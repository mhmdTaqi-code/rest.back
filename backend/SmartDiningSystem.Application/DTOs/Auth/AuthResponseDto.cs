namespace SmartDiningSystem.Application.DTOs.Auth;

public class AuthResponseDto
{
    public bool AccessGranted { get; set; }
    public bool PendingAdminReview { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? RejectionReason { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public UserSummaryDto? User { get; set; }
}
