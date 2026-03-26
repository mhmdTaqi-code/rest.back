namespace SmartDiningSystem.Application.DTOs.Auth;

public class RegisterOwnerRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string? RestaurantDescription { get; set; }
    public string RestaurantAddress { get; set; } = string.Empty;
    public string RestaurantPhoneNumber { get; set; } = string.Empty;
}
