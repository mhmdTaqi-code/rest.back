namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class ResolvedRestaurantTableDto
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid RestaurantTableId { get; set; }
    public int TableNumber { get; set; }
    public string TableDisplayName { get; set; } = string.Empty;
    public string TableToken { get; set; } = string.Empty;
    public bool RequiresAuthenticationForOrdering { get; set; } = true;
}
