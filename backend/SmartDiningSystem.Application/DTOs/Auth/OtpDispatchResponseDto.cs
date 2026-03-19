namespace SmartDiningSystem.Application.DTOs.Auth;

public class OtpDispatchResponseDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
