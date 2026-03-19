using System.ComponentModel.DataAnnotations;

namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class AdminRejectRestaurantRequestDto
{
    [Required]
    [MaxLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;
}
