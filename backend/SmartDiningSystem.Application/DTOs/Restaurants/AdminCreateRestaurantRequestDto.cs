using System.ComponentModel.DataAnnotations;

namespace SmartDiningSystem.Application.DTOs.Restaurants;

public class AdminCreateRestaurantRequestDto
{
    public Guid OwnerUserId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ImageUrl { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
