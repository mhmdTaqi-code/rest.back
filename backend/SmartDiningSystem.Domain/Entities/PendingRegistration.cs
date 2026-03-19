using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class PendingRegistration
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantDescription { get; set; }
    public string? RestaurantAddress { get; set; }
    public string? RestaurantPhoneNumber { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();
}
