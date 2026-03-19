using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class UserAccount
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsPhoneVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Restaurant> OwnedRestaurants { get; set; } = new List<Restaurant>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<OtpCode> LoginOtpCodes { get; set; } = new List<OtpCode>();
}
