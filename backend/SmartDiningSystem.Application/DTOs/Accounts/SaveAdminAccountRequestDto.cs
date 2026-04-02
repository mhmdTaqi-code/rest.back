using System.ComponentModel.DataAnnotations;

namespace SmartDiningSystem.Application.DTOs.Accounts;

public class SaveAdminAccountRequestDto
{
    [StringLength(200)]
    public string? FullName { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(50)]
    public string? Username { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsPhoneVerified { get; set; }

    [StringLength(200)]
    public string? RestaurantName { get; set; }

    [StringLength(1000)]
    public string? RestaurantDescription { get; set; }

    [StringLength(500)]
    public string? RestaurantAddress { get; set; }
}
