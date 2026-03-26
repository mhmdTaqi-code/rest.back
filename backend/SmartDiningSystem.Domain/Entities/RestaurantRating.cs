namespace SmartDiningSystem.Domain.Entities;

public class RestaurantRating
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public int Stars { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Restaurant? Restaurant { get; set; }
    public UserAccount? User { get; set; }
}
