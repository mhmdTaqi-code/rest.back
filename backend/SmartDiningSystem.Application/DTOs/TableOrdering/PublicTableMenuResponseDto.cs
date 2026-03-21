namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class PublicTableMenuResponseDto
{
    public ResolvedRestaurantTableDto Table { get; set; } = new();
    public IReadOnlyList<PublicTableMenuCategoryDto> Categories { get; set; } = Array.Empty<PublicTableMenuCategoryDto>();
}
