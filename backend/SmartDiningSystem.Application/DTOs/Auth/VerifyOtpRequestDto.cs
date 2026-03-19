namespace SmartDiningSystem.Application.DTOs.Auth;

public class VerifyOtpRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
