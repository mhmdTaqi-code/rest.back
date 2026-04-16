namespace SmartDiningSystem.Application.DTOs.TableAccess;

public class Team10CreatedOrderDto
{
    public Guid OrderId { get; set; }
    public int ItemsCount { get; set; }
    public decimal TotalPrice { get; set; }
}
